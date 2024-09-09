// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2023.

namespace Nu

// TODO: confirm this module/namespace arrangement is correct.

/// Force qualification of Vulkan namespace in Nu unless opened explicitly.
[<RequireQualifiedAccess>]
module Vulkan = let _ = ()

namespace Vulkan
open System
open System.Runtime.CompilerServices
open FSharp.NativeInterop
open SDL2
open Vortice.Vulkan
open type Vulkan
open Prime
open Nu

[<RequireQualifiedAccess>]
module Hl =

    // enable validation layers in debug mode
#if DEBUG
    let validationLayersEnabled = true
#else
    let validationLayersEnabled = false
#endif
    
    /// Check the given Vulkan operation result, logging on non-Success.
    let check (result : VkResult) =
        if int result > 0 then Log.info ("Vulkan info: " + string result)
        elif int result < 0 then Log.error ("Vulkan error: " + string result)
    
    /// Convert VkLayerProperties.layerName to a string.
    let getLayerName (layerProps : VkLayerProperties) =
        let mutable layerName = layerProps.layerName
        let ptr = asBytePointer &layerName
        let vkUtf8Str = new VkUtf8String (ptr)
        vkUtf8Str.ToString ()
    
    /// A container for a pinned array of unmanaged strings.
    type StringArrayWrap private (array : nativeptr<byte> array) =
    
        let array = array
        let pin = ArrayPin array
    
        new (strs : string array) =
            let ptrs = Array.zeroCreate<nativeptr<byte>> strs.Length
            for i in [0 .. dec strs.Length] do ptrs[i] <- VkStringInterop.ConvertToUnmanaged strs[i]
            new StringArrayWrap (ptrs)
    
        // TODO: see if implicit conversion can be used to remove the need to call this member directly.
        member this.Pointer = pin.Pointer

        // make disposal publicly available without casting
        member this.Dispose () = pin.Dispose ()
    
        interface IDisposable with
            member this.Dispose () =
                this.Dispose ()
    
    /// The Vulkan handles that must be globally accessible within the renderer.
    type [<ReferenceEquality>] VulkanGlobal =
        private
            { Instance : VkInstance
              Surface : VkSurfaceKHR }

        /// Create the Vulkan instance.
        static member createInstance window =

            // instance handle
            let mutable instance = Unchecked.defaultof<VkInstance>

            // get sdl extensions
            let mutable sdlExtensionCount = 0u
            let result = SDL.SDL_Vulkan_GetInstanceExtensions (window, &sdlExtensionCount, null)
            if int result = 0 then Log.error "SDL error, SDL_Vulkan_GetInstanceExtensions failed."
            let sdlExtensionsOut = Array.zeroCreate<nativeint> (int sdlExtensionCount)
            let result = SDL.SDL_Vulkan_GetInstanceExtensions (window, &sdlExtensionCount, sdlExtensionsOut)
            if int result = 0 then Log.error "SDL error, SDL_Vulkan_GetInstanceExtensions failed."
            let sdlExtensions = Array.zeroCreate<nativeptr<byte>> (int sdlExtensionCount)
            for i in [0 .. dec (int sdlExtensionCount)] do sdlExtensions[i] <- NativePtr.ofNativeInt<byte> sdlExtensionsOut[i]
            use sdlExtensionsPin = ArrayPin sdlExtensions
            
            // TODO: setup message callback with debug utils *if* motivation arises.
            
            // get available instance layers
            let mutable layerCount = 0u
            vkEnumerateInstanceLayerProperties (asPointer &layerCount, NativePtr.nullPtr) |> check
            let layers = Array.zeroCreate<VkLayerProperties> (int layerCount)
            use layersPin = ArrayPin layers
            vkEnumerateInstanceLayerProperties (asPointer &layerCount, layersPin.Pointer) |> check

            // check if validation layer exists
            let validationLayer = "VK_LAYER_KHRONOS_validation"
            let validationLayerExists = Array.exists (fun x -> getLayerName x = validationLayer) layers
            if validationLayersEnabled && not validationLayerExists then Log.info (validationLayer + " is not available. Vulkan programmers must install the Vulkan SDK to enable validation.")
            
            // TODO: apply VkApplicationInfo once all compulsory fields have been decided (e.g. engineVersion)
            // and check for available vulkan version as described in 
            // https://registry.khronos.org/vulkan/specs/1.3-extensions/html/chap4.html#VkApplicationInfo.

            // populate createinstance info
            let mutable createInfo = VkInstanceCreateInfo ()
            createInfo.enabledExtensionCount <- sdlExtensionCount
            createInfo.ppEnabledExtensionNames <- sdlExtensionsPin.Pointer

            // load validation layer if enabled and available
            if validationLayersEnabled && validationLayerExists then
                use layerWrap = StringArrayWrap [|validationLayer|]
                createInfo.enabledLayerCount <- 1u
                createInfo.ppEnabledLayerNames <- layerWrap.Pointer
            else
                createInfo.enabledLayerCount <- 0u

            // create instance
            vkCreateInstance (&createInfo, NativePtr.nullPtr, &instance) |> check

            // fin
            instance
        
        /// Create surface.
        static member createSurface window instance =

            // surface handle
            let mutable surface = Unchecked.defaultof<VkSurfaceKHR>

            // get surface from sdl
            let result = SDL.SDL_Vulkan_CreateSurface (window, instance, &(Unsafe.As<VkSurfaceKHR, uint64> &surface))
            if int result = 0 then Log.error "SDL error, SDL_Vulkan_CreateSurface failed."

            // fin
            surface
        
        /// Select compatible physical device if available.
        static member trySelectPhysicalDevice surface instance =
            
            // get available physical devices
            let mutable deviceCount = 0u
            vkEnumeratePhysicalDevices (instance, asPointer &deviceCount, NativePtr.nullPtr) |> check
            let devices = Array.zeroCreate<VkPhysicalDevice> (int deviceCount)
            use devicesPin = ArrayPin devices
            vkEnumeratePhysicalDevices (instance, asPointer &deviceCount, devicesPin.Pointer) |> check

            // get the devices' props
            let generalProps = Array.zeroCreate<VkPhysicalDeviceProperties> devices.Length
            for i in [0 .. dec devices.Length] do
                let mutable props = Unchecked.defaultof<VkPhysicalDeviceProperties>
                vkGetPhysicalDeviceProperties (devices[i], &props)
                generalProps[i] <- props

            // get the devices' queue families' props
            let queueFamilyProps = Array.zeroCreate<VkQueueFamilyProperties array> devices.Length
            for i in [0 .. dec devices.Length] do
                let mutable queueFamilyCount = 0u
                vkGetPhysicalDeviceQueueFamilyProperties (devices[i], asPointer &queueFamilyCount, NativePtr.nullPtr)
                let queueFamilies = Array.zeroCreate<VkQueueFamilyProperties> (int queueFamilyCount)
                use queueFamiliesPin = ArrayPin queueFamilies
                vkGetPhysicalDeviceQueueFamilyProperties (devices[i], asPointer &queueFamilyCount, queueFamiliesPin.Pointer)
                queueFamilyProps[i] <- queueFamilies

            // try find graphics and present queue families
            let queueFamilyOpts = Array.zeroCreate<uint option * uint option> devices.Length
            for i in [0 .. dec devices.Length] do
                
                (* It is *essential* to use the *first* compatible queue families in the array, *not* the last, as per the tutorial and vortice vulkan sample.
                   I discovered this by accident because the queue families on my AMD behaved exactly the same as the queue families on this one:

                   https://computergraphics.stackexchange.com/questions/9707/queue-from-a-family-queue-that-supports-presentation-doesnt-work-vulkan

                   general lesson: trust level for vendors is too low for deviation from common practices to be advisable. *)
                
                let mutable graphicsQueueFamilyOpt = None
                let mutable presentQueueFamilyOpt = None
                for j in [0 .. dec queueFamilyProps[i].Length] do
                    
                    // try get graphics queue family
                    match graphicsQueueFamilyOpt with
                    | None ->
                        let queueFamily = queueFamilyProps[i][j]
                        if queueFamily.queueFlags &&& VkQueueFlags.Graphics <> VkQueueFlags.None then
                            graphicsQueueFamilyOpt <- Some (uint j)
                    | Some _ -> ()

                    // try get present queue family
                    match presentQueueFamilyOpt with
                    | None ->
                        let mutable presentSupport = VkBool32.False
                        vkGetPhysicalDeviceSurfaceSupportKHR (devices[i], uint j, surface, &presentSupport) |> check
                        if (presentSupport = VkBool32.True) then
                            presentQueueFamilyOpt <- Some (uint j)
                    | Some _ -> ()

                queueFamilyOpts[i] <- (graphicsQueueFamilyOpt, presentQueueFamilyOpt)

            // gather devices together with relevant data for selection
            let candidates = [ for i in [0 .. dec devices.Length] -> (devices[i], generalProps[i], queueFamilyOpts[i]) ]

            // compatibility criteria: device must support the essential queue operations and Vulkan 1.3
            let isCompatible (_, props : VkPhysicalDeviceProperties, queueFamilyOpts) =
                Option.isSome (fst queueFamilyOpts) &&
                Option.isSome (snd queueFamilyOpts) &&
                props.apiVersion.Minor >= 3u

            // preferability criteria: device ought to be discrete
            let isPreferable (_, props : VkPhysicalDeviceProperties, _) = props.deviceType = VK_PHYSICAL_DEVICE_TYPE_DISCRETE_GPU
            
            // filter and order candidates according to criteria
            let candidatesFiltered = List.filter isCompatible candidates
            let (fstChoice, sndChoice) = List.partition isPreferable candidatesFiltered
            let candidatesFilteredAndOrdered = List.append fstChoice sndChoice
                
            // if compatible devices exist then return the first along with its queue families
            let physicalDeviceOpt =
                if candidatesFilteredAndOrdered.Length > 0 then
                    let (physicalDevice, _, queueFamilyOpts) = List.head candidatesFilteredAndOrdered
                    let graphicsQueueFamily = fst queueFamilyOpts |> Option.get
                    let presentQueueFamily = snd queueFamilyOpts |> Option.get
                    Some (physicalDevice, graphicsQueueFamily, presentQueueFamily)
                else
                    Log.info "Could not find a suitable graphics device for Vulkan."
                    None

            // fin
            physicalDeviceOpt
        
        /// Destroy Vulkan handles.
        static member cleanup vulkanGlobal =
            vkDestroySurfaceKHR (vulkanGlobal.Instance, vulkanGlobal.Surface, NativePtr.nullPtr)
            vkDestroyInstance (vulkanGlobal.Instance, NativePtr.nullPtr)
        
        /// Try to make a VulkanGlobal.
        static member tryMake window =

            // loads vulkan; not vulkan function
            vkInitialize () |> check

            // create instance
            let instance = VulkanGlobal.createInstance window

            // loads instance commands; not vulkan function
            vkLoadInstanceOnly instance

            // create surface
            let surface = VulkanGlobal.createSurface window instance
            
            // try select physical device
            match VulkanGlobal.trySelectPhysicalDevice surface instance with
            | Some (physicalDevice, graphicsQueueFamily, presentQueueFamily) ->

                // make vulkanGlobal
                let vulkanGlobal =
                    { Instance = instance
                      Surface = surface }

                // fin
                Some vulkanGlobal

            // abort vulkan
            | None -> None