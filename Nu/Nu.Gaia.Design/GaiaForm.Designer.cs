﻿namespace Nu.Gaia.Design
{
    partial class GaiaForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GaiaForm));
			this.menuStrip = new System.Windows.Forms.MenuStrip();
			this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.createContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.deleteContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
			this.cutContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.copyContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.pasteContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.splitContainer4 = new System.Windows.Forms.SplitContainer();
			this.splitContainer8 = new System.Windows.Forms.SplitContainer();
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.tabPage3 = new System.Windows.Forms.TabPage();
			this.hierarchyTreeView = new System.Windows.Forms.TreeView();
			this.splitContainer9 = new System.Windows.Forms.SplitContainer();
			this.groupTabControl = new System.Windows.Forms.TabControl();
			this.tabPage = new System.Windows.Forms.TabPage();
			this.displayPanel = new Nu.Gaia.Design.SelectablePanel();
			this.rolloutTabControl = new System.Windows.Forms.TabControl();
			this.propertyEditorTabPage = new System.Windows.Forms.TabPage();
			this.propertyEditor = new System.Windows.Forms.SplitContainer();
			this.splitContainer5 = new System.Windows.Forms.SplitContainer();
			this.pickPropertyButton = new System.Windows.Forms.Button();
			this.applyPropertyButton = new System.Windows.Forms.Button();
			this.propertyNameLabel = new System.Windows.Forms.Label();
			this.discardPropertyButton = new System.Windows.Forms.Button();
			this.propertyDescriptionTextBox = new System.Windows.Forms.TextBox();
			this.propertyValueTextBox = new Nu.Gaia.Design.SymbolicTextBox();
			this.assetGraphTabPage = new System.Windows.Forms.TabPage();
			this.assetGraph = new System.Windows.Forms.SplitContainer();
			this.discardAssetGraphButton = new System.Windows.Forms.Button();
			this.applyAssetGraphButton = new System.Windows.Forms.Button();
			this.assetGraphTextBox = new Nu.Gaia.Design.SymbolicTextBox();
			this.overlayTabPage = new System.Windows.Forms.TabPage();
			this.overlayer = new System.Windows.Forms.SplitContainer();
			this.discardOverlayerButton = new System.Windows.Forms.Button();
			this.applyOverlayerButton = new System.Windows.Forms.Button();
			this.overlayerTextBox = new Nu.Gaia.Design.SymbolicTextBox();
			this.evaluatorTabPage = new System.Windows.Forms.TabPage();
			this.terminal = new System.Windows.Forms.SplitContainer();
			this.evalInputTextBox = new Nu.Gaia.Design.SymbolicTextBox();
			this.splitContainer10 = new System.Windows.Forms.SplitContainer();
			this.evalLineButton = new System.Windows.Forms.Button();
			this.clearOutputButton = new System.Windows.Forms.Button();
			this.evalButton = new System.Windows.Forms.Button();
			this.evalOutputTextBox = new Nu.Gaia.Design.SymbolicTextBox();
			this.preludeTabPage = new System.Windows.Forms.TabPage();
			this.splitContainer6 = new System.Windows.Forms.SplitContainer();
			this.discardPreludeButton = new System.Windows.Forms.Button();
			this.applyPreludeButton = new System.Windows.Forms.Button();
			this.preludeTextBox = new Nu.Gaia.Design.SymbolicTextBox();
			this.eventTracingTabPage = new System.Windows.Forms.TabPage();
			this.eventTracing = new System.Windows.Forms.SplitContainer();
			this.discardEventFilterButton = new System.Windows.Forms.Button();
			this.traceEventsCheckBox = new System.Windows.Forms.CheckBox();
			this.applyEventFilterButton = new System.Windows.Forms.Button();
			this.eventFilterTextBox = new Nu.Gaia.Design.SymbolicTextBox();
			this.propertyTabControl = new System.Windows.Forms.TabControl();
			this.entityTabPage = new System.Windows.Forms.TabPage();
			this.splitContainer7 = new System.Windows.Forms.SplitContainer();
			this.entityPropertyDesigner = new System.Windows.Forms.GroupBox();
			this.entityIgnorePropertyBindingsCheckBox = new System.Windows.Forms.CheckBox();
			this.entityDesignerPropertyRemoveButton = new System.Windows.Forms.Button();
			this.entityDesignerPropertyNameTextBox = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.entityDesignerPropertyTypeComboBox = new System.Windows.Forms.ComboBox();
			this.label1 = new System.Windows.Forms.Label();
			this.entityDesignerPropertyDefaultButton = new System.Windows.Forms.Button();
			this.entityDesignerPropertyAddButton = new System.Windows.Forms.Button();
			this.panel1 = new System.Windows.Forms.Panel();
			this.entityPropertyGrid = new System.Windows.Forms.PropertyGrid();
			this.groupTabPage = new System.Windows.Forms.TabPage();
			this.groupPropertyGrid = new System.Windows.Forms.PropertyGrid();
			this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
			this.undoButton = new System.Windows.Forms.ToolStripButton();
			this.redoButton = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
			this.positionSnapLabel = new System.Windows.Forms.ToolStripLabel();
			this.positionSnapTextBox = new System.Windows.Forms.ToolStripTextBox();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.rotationSnapLabel = new System.Windows.Forms.ToolStripLabel();
			this.rotationSnapTextBox = new System.Windows.Forms.ToolStripTextBox();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.createEntityButton = new System.Windows.Forms.ToolStripButton();
			this.createEntityComboBox = new System.Windows.Forms.ToolStripComboBox();
			this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
			this.overlayComboBox = new System.Windows.Forms.ToolStripComboBox();
			this.creationElevationLabel = new System.Windows.Forms.ToolStripLabel();
			this.createElevationMinusButton = new System.Windows.Forms.ToolStripButton();
			this.createElevationTextBox = new System.Windows.Forms.ToolStripTextBox();
			this.createElevationPlusButton = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.quickSizeToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.deleteEntityButton = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.resetCameraButton = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
			this.reloadAssetsButton = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator9 = new System.Windows.Forms.ToolStripSeparator();
			this.advancingButton = new System.Windows.Forms.ToolStripButton();
			this.editWhileInteractiveCheckBox = new System.Windows.Forms.ToolStripButton();
			this.toolStrip = new System.Windows.Forms.ToolStrip();
			this.fileDropDownButton = new System.Windows.Forms.ToolStripDropDownButton();
			this.newGroupToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openGroupToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveGroupToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveGroupAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem7 = new System.Windows.Forms.ToolStripSeparator();
			this.clearGroupToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.closeGroupToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem8 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.editDropDownButton = new System.Windows.Forms.ToolStripDropDownButton();
			this.undoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.redoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem9 = new System.Windows.Forms.ToolStripSeparator();
			this.cutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.pasteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem10 = new System.Windows.Forms.ToolStripSeparator();
			this.createToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.quickSizeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem11 = new System.Windows.Forms.ToolStripSeparator();
			this.startStopAdvancingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem12 = new System.Windows.Forms.ToolStripSeparator();
			this.changeGroupNameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
			this.songPlaybackButton = new System.Windows.Forms.ToolStripButton();
			this.contextMenuStrip.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer4)).BeginInit();
			this.splitContainer4.Panel1.SuspendLayout();
			this.splitContainer4.Panel2.SuspendLayout();
			this.splitContainer4.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer8)).BeginInit();
			this.splitContainer8.Panel1.SuspendLayout();
			this.splitContainer8.Panel2.SuspendLayout();
			this.splitContainer8.SuspendLayout();
			this.tabControl1.SuspendLayout();
			this.tabPage3.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer9)).BeginInit();
			this.splitContainer9.Panel1.SuspendLayout();
			this.splitContainer9.Panel2.SuspendLayout();
			this.splitContainer9.SuspendLayout();
			this.groupTabControl.SuspendLayout();
			this.rolloutTabControl.SuspendLayout();
			this.propertyEditorTabPage.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.propertyEditor)).BeginInit();
			this.propertyEditor.Panel1.SuspendLayout();
			this.propertyEditor.Panel2.SuspendLayout();
			this.propertyEditor.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer5)).BeginInit();
			this.splitContainer5.Panel1.SuspendLayout();
			this.splitContainer5.Panel2.SuspendLayout();
			this.splitContainer5.SuspendLayout();
			this.assetGraphTabPage.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.assetGraph)).BeginInit();
			this.assetGraph.Panel1.SuspendLayout();
			this.assetGraph.Panel2.SuspendLayout();
			this.assetGraph.SuspendLayout();
			this.overlayTabPage.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.overlayer)).BeginInit();
			this.overlayer.Panel1.SuspendLayout();
			this.overlayer.Panel2.SuspendLayout();
			this.overlayer.SuspendLayout();
			this.evaluatorTabPage.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.terminal)).BeginInit();
			this.terminal.Panel1.SuspendLayout();
			this.terminal.Panel2.SuspendLayout();
			this.terminal.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer10)).BeginInit();
			this.splitContainer10.Panel1.SuspendLayout();
			this.splitContainer10.Panel2.SuspendLayout();
			this.splitContainer10.SuspendLayout();
			this.preludeTabPage.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer6)).BeginInit();
			this.splitContainer6.Panel1.SuspendLayout();
			this.splitContainer6.Panel2.SuspendLayout();
			this.splitContainer6.SuspendLayout();
			this.eventTracingTabPage.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.eventTracing)).BeginInit();
			this.eventTracing.Panel1.SuspendLayout();
			this.eventTracing.Panel2.SuspendLayout();
			this.eventTracing.SuspendLayout();
			this.propertyTabControl.SuspendLayout();
			this.entityTabPage.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer7)).BeginInit();
			this.splitContainer7.Panel1.SuspendLayout();
			this.splitContainer7.Panel2.SuspendLayout();
			this.splitContainer7.SuspendLayout();
			this.entityPropertyDesigner.SuspendLayout();
			this.panel1.SuspendLayout();
			this.groupTabPage.SuspendLayout();
			this.toolStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// menuStrip
			// 
			this.menuStrip.Enabled = false;
			this.menuStrip.Font = new System.Drawing.Font("Segoe UI", 10F);
			this.menuStrip.Location = new System.Drawing.Point(0, 0);
			this.menuStrip.Name = "menuStrip";
			this.menuStrip.Padding = new System.Windows.Forms.Padding(4, 2, 0, 2);
			this.menuStrip.Size = new System.Drawing.Size(1276, 24);
			this.menuStrip.TabIndex = 0;
			this.menuStrip.Text = "menuStrip1";
			// 
			// contextMenuStrip
			// 
			this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.createContextMenuItem,
            this.deleteContextMenuItem,
            this.toolStripMenuItem3,
            this.cutContextMenuItem,
            this.copyContextMenuItem,
            this.pasteContextMenuItem});
			this.contextMenuStrip.Name = "contextMenuStrip";
			this.contextMenuStrip.Size = new System.Drawing.Size(117, 120);
			// 
			// createContextMenuItem
			// 
			this.createContextMenuItem.Name = "createContextMenuItem";
			this.createContextMenuItem.Size = new System.Drawing.Size(116, 22);
			this.createContextMenuItem.Text = "Cr[&e]ate";
			// 
			// deleteContextMenuItem
			// 
			this.deleteContextMenuItem.Name = "deleteContextMenuItem";
			this.deleteContextMenuItem.Size = new System.Drawing.Size(116, 22);
			this.deleteContextMenuItem.Text = "[&D]elete";
			// 
			// toolStripMenuItem3
			// 
			this.toolStripMenuItem3.Name = "toolStripMenuItem3";
			this.toolStripMenuItem3.Size = new System.Drawing.Size(113, 6);
			// 
			// cutContextMenuItem
			// 
			this.cutContextMenuItem.Name = "cutContextMenuItem";
			this.cutContextMenuItem.Size = new System.Drawing.Size(116, 22);
			this.cutContextMenuItem.Text = "C[&u]t";
			// 
			// copyContextMenuItem
			// 
			this.copyContextMenuItem.Name = "copyContextMenuItem";
			this.copyContextMenuItem.Size = new System.Drawing.Size(116, 22);
			this.copyContextMenuItem.Text = "[&C]opy";
			// 
			// pasteContextMenuItem
			// 
			this.pasteContextMenuItem.Name = "pasteContextMenuItem";
			this.pasteContextMenuItem.Size = new System.Drawing.Size(116, 22);
			this.pasteContextMenuItem.Text = "[&P]aste";
			// 
			// splitContainer1
			// 
			this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
			this.splitContainer1.Location = new System.Drawing.Point(0, 24);
			this.splitContainer1.Margin = new System.Windows.Forms.Padding(2);
			this.splitContainer1.Name = "splitContainer1";
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.splitContainer4);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.propertyTabControl);
			this.splitContainer1.Size = new System.Drawing.Size(1276, 669);
			this.splitContainer1.SplitterDistance = 964;
			this.splitContainer1.SplitterWidth = 3;
			this.splitContainer1.TabIndex = 3;
			// 
			// splitContainer4
			// 
			this.splitContainer4.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer4.Location = new System.Drawing.Point(0, 0);
			this.splitContainer4.Name = "splitContainer4";
			this.splitContainer4.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer4.Panel1
			// 
			this.splitContainer4.Panel1.Controls.Add(this.splitContainer8);
			// 
			// splitContainer4.Panel2
			// 
			this.splitContainer4.Panel2.Controls.Add(this.rolloutTabControl);
			this.splitContainer4.Size = new System.Drawing.Size(964, 669);
			this.splitContainer4.SplitterDistance = 454;
			this.splitContainer4.TabIndex = 1;
			// 
			// splitContainer8
			// 
			this.splitContainer8.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer8.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.splitContainer8.Location = new System.Drawing.Point(0, 0);
			this.splitContainer8.Name = "splitContainer8";
			// 
			// splitContainer8.Panel1
			// 
			this.splitContainer8.Panel1.Controls.Add(this.tabControl1);
			// 
			// splitContainer8.Panel2
			// 
			this.splitContainer8.Panel2.Controls.Add(this.splitContainer9);
			this.splitContainer8.Size = new System.Drawing.Size(964, 454);
			this.splitContainer8.SplitterDistance = 265;
			this.splitContainer8.TabIndex = 5;
			// 
			// tabControl1
			// 
			this.tabControl1.Controls.Add(this.tabPage3);
			this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControl1.Location = new System.Drawing.Point(0, 0);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(265, 454);
			this.tabControl1.TabIndex = 2;
			// 
			// tabPage3
			// 
			this.tabPage3.Controls.Add(this.hierarchyTreeView);
			this.tabPage3.Location = new System.Drawing.Point(4, 22);
			this.tabPage3.Name = "tabPage3";
			this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage3.Size = new System.Drawing.Size(257, 428);
			this.tabPage3.TabIndex = 1;
			this.tabPage3.Text = "Hierarchy";
			this.tabPage3.UseVisualStyleBackColor = true;
			// 
			// hierarchyTreeView
			// 
			this.hierarchyTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.hierarchyTreeView.HideSelection = false;
			this.hierarchyTreeView.Location = new System.Drawing.Point(3, 3);
			this.hierarchyTreeView.Name = "hierarchyTreeView";
			this.hierarchyTreeView.Size = new System.Drawing.Size(251, 422);
			this.hierarchyTreeView.TabIndex = 0;
			// 
			// splitContainer9
			// 
			this.splitContainer9.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer9.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.splitContainer9.IsSplitterFixed = true;
			this.splitContainer9.Location = new System.Drawing.Point(0, 0);
			this.splitContainer9.Name = "splitContainer9";
			this.splitContainer9.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer9.Panel1
			// 
			this.splitContainer9.Panel1.Controls.Add(this.groupTabControl);
			// 
			// splitContainer9.Panel2
			// 
			this.splitContainer9.Panel2.Controls.Add(this.displayPanel);
			this.splitContainer9.Size = new System.Drawing.Size(695, 454);
			this.splitContainer9.SplitterDistance = 25;
			this.splitContainer9.TabIndex = 0;
			// 
			// groupTabControl
			// 
			this.groupTabControl.Controls.Add(this.tabPage);
			this.groupTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.groupTabControl.Location = new System.Drawing.Point(0, 0);
			this.groupTabControl.Name = "groupTabControl";
			this.groupTabControl.SelectedIndex = 0;
			this.groupTabControl.Size = new System.Drawing.Size(695, 25);
			this.groupTabControl.TabIndex = 4;
			// 
			// tabPage
			// 
			this.tabPage.Location = new System.Drawing.Point(4, 22);
			this.tabPage.Name = "tabPage";
			this.tabPage.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage.Size = new System.Drawing.Size(687, 0);
			this.tabPage.TabIndex = 0;
			this.tabPage.UseVisualStyleBackColor = true;
			// 
			// displayPanel
			// 
			this.displayPanel.ContextMenuStrip = this.contextMenuStrip;
			this.displayPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.displayPanel.Location = new System.Drawing.Point(0, 0);
			this.displayPanel.Margin = new System.Windows.Forms.Padding(2);
			this.displayPanel.Name = "displayPanel";
			this.displayPanel.Size = new System.Drawing.Size(695, 425);
			this.displayPanel.TabIndex = 0;
			this.displayPanel.TabStop = true;
			// 
			// rolloutTabControl
			// 
			this.rolloutTabControl.Controls.Add(this.propertyEditorTabPage);
			this.rolloutTabControl.Controls.Add(this.assetGraphTabPage);
			this.rolloutTabControl.Controls.Add(this.overlayTabPage);
			this.rolloutTabControl.Controls.Add(this.evaluatorTabPage);
			this.rolloutTabControl.Controls.Add(this.preludeTabPage);
			this.rolloutTabControl.Controls.Add(this.eventTracingTabPage);
			this.rolloutTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.rolloutTabControl.Location = new System.Drawing.Point(0, 0);
			this.rolloutTabControl.Name = "rolloutTabControl";
			this.rolloutTabControl.SelectedIndex = 0;
			this.rolloutTabControl.Size = new System.Drawing.Size(964, 211);
			this.rolloutTabControl.TabIndex = 0;
			// 
			// propertyEditorTabPage
			// 
			this.propertyEditorTabPage.Controls.Add(this.propertyEditor);
			this.propertyEditorTabPage.Location = new System.Drawing.Point(4, 22);
			this.propertyEditorTabPage.Name = "propertyEditorTabPage";
			this.propertyEditorTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.propertyEditorTabPage.Size = new System.Drawing.Size(956, 185);
			this.propertyEditorTabPage.TabIndex = 0;
			this.propertyEditorTabPage.Text = "[P]roperty Editor";
			this.propertyEditorTabPage.UseVisualStyleBackColor = true;
			// 
			// propertyEditor
			// 
			this.propertyEditor.Dock = System.Windows.Forms.DockStyle.Fill;
			this.propertyEditor.Enabled = false;
			this.propertyEditor.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.propertyEditor.IsSplitterFixed = true;
			this.propertyEditor.Location = new System.Drawing.Point(3, 3);
			this.propertyEditor.Name = "propertyEditor";
			// 
			// propertyEditor.Panel1
			// 
			this.propertyEditor.Panel1.Controls.Add(this.splitContainer5);
			// 
			// propertyEditor.Panel2
			// 
			this.propertyEditor.Panel2.Controls.Add(this.propertyValueTextBox);
			this.propertyEditor.Size = new System.Drawing.Size(950, 179);
			this.propertyEditor.SplitterDistance = 176;
			this.propertyEditor.TabIndex = 0;
			// 
			// splitContainer5
			// 
			this.splitContainer5.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer5.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.splitContainer5.IsSplitterFixed = true;
			this.splitContainer5.Location = new System.Drawing.Point(0, 0);
			this.splitContainer5.Name = "splitContainer5";
			this.splitContainer5.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer5.Panel1
			// 
			this.splitContainer5.Panel1.Controls.Add(this.pickPropertyButton);
			this.splitContainer5.Panel1.Controls.Add(this.applyPropertyButton);
			this.splitContainer5.Panel1.Controls.Add(this.propertyNameLabel);
			this.splitContainer5.Panel1.Controls.Add(this.discardPropertyButton);
			// 
			// splitContainer5.Panel2
			// 
			this.splitContainer5.Panel2.Controls.Add(this.propertyDescriptionTextBox);
			this.splitContainer5.Size = new System.Drawing.Size(176, 179);
			this.splitContainer5.SplitterDistance = 98;
			this.splitContainer5.TabIndex = 0;
			// 
			// pickPropertyButton
			// 
			this.pickPropertyButton.Location = new System.Drawing.Point(121, 6);
			this.pickPropertyButton.Name = "pickPropertyButton";
			this.pickPropertyButton.Size = new System.Drawing.Size(42, 23);
			this.pickPropertyButton.TabIndex = 1;
			this.pickPropertyButton.Text = "Pic[k]";
			this.pickPropertyButton.UseVisualStyleBackColor = true;
			this.pickPropertyButton.Visible = false;
			// 
			// applyPropertyButton
			// 
			this.applyPropertyButton.Location = new System.Drawing.Point(16, 35);
			this.applyPropertyButton.Name = "applyPropertyButton";
			this.applyPropertyButton.Size = new System.Drawing.Size(147, 23);
			this.applyPropertyButton.TabIndex = 2;
			this.applyPropertyButton.Text = "[A]pply Change";
			this.applyPropertyButton.UseVisualStyleBackColor = true;
			// 
			// propertyNameLabel
			// 
			this.propertyNameLabel.AutoSize = true;
			this.propertyNameLabel.Location = new System.Drawing.Point(13, 11);
			this.propertyNameLabel.Name = "propertyNameLabel";
			this.propertyNameLabel.Size = new System.Drawing.Size(0, 13);
			this.propertyNameLabel.TabIndex = 0;
			// 
			// discardPropertyButton
			// 
			this.discardPropertyButton.Location = new System.Drawing.Point(16, 64);
			this.discardPropertyButton.Name = "discardPropertyButton";
			this.discardPropertyButton.Size = new System.Drawing.Size(147, 23);
			this.discardPropertyButton.TabIndex = 3;
			this.discardPropertyButton.Text = "[D]iscard Change";
			this.discardPropertyButton.UseVisualStyleBackColor = true;
			// 
			// propertyDescriptionTextBox
			// 
			this.propertyDescriptionTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.propertyDescriptionTextBox.Location = new System.Drawing.Point(0, 0);
			this.propertyDescriptionTextBox.Multiline = true;
			this.propertyDescriptionTextBox.Name = "propertyDescriptionTextBox";
			this.propertyDescriptionTextBox.ReadOnly = true;
			this.propertyDescriptionTextBox.Size = new System.Drawing.Size(176, 77);
			this.propertyDescriptionTextBox.TabIndex = 0;
			// 
			// propertyValueTextBox
			// 
			this.propertyValueTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.propertyValueTextBox.ExtraDescent = 1;
			this.propertyValueTextBox.Keywords0 = "";
			this.propertyValueTextBox.Keywords1 = "";
			this.propertyValueTextBox.KeywordsImplicit = "True False Some None Right Left";
			this.propertyValueTextBox.Lexer = ScintillaNET.Lexer.Lisp;
			this.propertyValueTextBox.Location = new System.Drawing.Point(0, 0);
			this.propertyValueTextBox.Name = "propertyValueTextBox";
			this.propertyValueTextBox.Size = new System.Drawing.Size(770, 179);
			this.propertyValueTextBox.TabIndex = 0;
			// 
			// assetGraphTabPage
			// 
			this.assetGraphTabPage.Controls.Add(this.assetGraph);
			this.assetGraphTabPage.Location = new System.Drawing.Point(4, 22);
			this.assetGraphTabPage.Name = "assetGraphTabPage";
			this.assetGraphTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.assetGraphTabPage.Size = new System.Drawing.Size(956, 185);
			this.assetGraphTabPage.TabIndex = 3;
			this.assetGraphTabPage.Text = "Asset Graph";
			this.assetGraphTabPage.UseVisualStyleBackColor = true;
			// 
			// assetGraph
			// 
			this.assetGraph.Dock = System.Windows.Forms.DockStyle.Fill;
			this.assetGraph.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.assetGraph.IsSplitterFixed = true;
			this.assetGraph.Location = new System.Drawing.Point(3, 3);
			this.assetGraph.Name = "assetGraph";
			// 
			// assetGraph.Panel1
			// 
			this.assetGraph.Panel1.Controls.Add(this.discardAssetGraphButton);
			this.assetGraph.Panel1.Controls.Add(this.applyAssetGraphButton);
			// 
			// assetGraph.Panel2
			// 
			this.assetGraph.Panel2.Controls.Add(this.assetGraphTextBox);
			this.assetGraph.Size = new System.Drawing.Size(950, 179);
			this.assetGraph.SplitterDistance = 176;
			this.assetGraph.TabIndex = 2;
			// 
			// discardAssetGraphButton
			// 
			this.discardAssetGraphButton.Location = new System.Drawing.Point(16, 41);
			this.discardAssetGraphButton.Name = "discardAssetGraphButton";
			this.discardAssetGraphButton.Size = new System.Drawing.Size(147, 23);
			this.discardAssetGraphButton.TabIndex = 4;
			this.discardAssetGraphButton.Text = "[D]iscard Changes";
			this.discardAssetGraphButton.UseVisualStyleBackColor = true;
			// 
			// applyAssetGraphButton
			// 
			this.applyAssetGraphButton.Location = new System.Drawing.Point(16, 12);
			this.applyAssetGraphButton.Name = "applyAssetGraphButton";
			this.applyAssetGraphButton.Size = new System.Drawing.Size(147, 23);
			this.applyAssetGraphButton.TabIndex = 2;
			this.applyAssetGraphButton.Text = "[A]pply Changes";
			this.applyAssetGraphButton.UseVisualStyleBackColor = true;
			// 
			// assetGraphTextBox
			// 
			this.assetGraphTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.assetGraphTextBox.ExtraDescent = 1;
			this.assetGraphTextBox.Keywords0 = "";
			this.assetGraphTextBox.Keywords1 = "";
			this.assetGraphTextBox.KeywordsImplicit = "True False Some None Right Left";
			this.assetGraphTextBox.Lexer = ScintillaNET.Lexer.Lisp;
			this.assetGraphTextBox.Location = new System.Drawing.Point(0, 0);
			this.assetGraphTextBox.Name = "assetGraphTextBox";
			this.assetGraphTextBox.Size = new System.Drawing.Size(770, 179);
			this.assetGraphTextBox.TabIndex = 0;
			// 
			// overlayTabPage
			// 
			this.overlayTabPage.Controls.Add(this.overlayer);
			this.overlayTabPage.Location = new System.Drawing.Point(4, 22);
			this.overlayTabPage.Name = "overlayTabPage";
			this.overlayTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.overlayTabPage.Size = new System.Drawing.Size(956, 185);
			this.overlayTabPage.TabIndex = 2;
			this.overlayTabPage.Text = "Overlay";
			this.overlayTabPage.UseVisualStyleBackColor = true;
			// 
			// overlayer
			// 
			this.overlayer.Dock = System.Windows.Forms.DockStyle.Fill;
			this.overlayer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.overlayer.IsSplitterFixed = true;
			this.overlayer.Location = new System.Drawing.Point(3, 3);
			this.overlayer.Name = "overlayer";
			// 
			// overlayer.Panel1
			// 
			this.overlayer.Panel1.Controls.Add(this.discardOverlayerButton);
			this.overlayer.Panel1.Controls.Add(this.applyOverlayerButton);
			// 
			// overlayer.Panel2
			// 
			this.overlayer.Panel2.Controls.Add(this.overlayerTextBox);
			this.overlayer.Size = new System.Drawing.Size(950, 179);
			this.overlayer.SplitterDistance = 176;
			this.overlayer.TabIndex = 1;
			// 
			// discardOverlayerButton
			// 
			this.discardOverlayerButton.Location = new System.Drawing.Point(16, 41);
			this.discardOverlayerButton.Name = "discardOverlayerButton";
			this.discardOverlayerButton.Size = new System.Drawing.Size(147, 23);
			this.discardOverlayerButton.TabIndex = 4;
			this.discardOverlayerButton.Text = "[D]iscard Changes";
			this.discardOverlayerButton.UseVisualStyleBackColor = true;
			// 
			// applyOverlayerButton
			// 
			this.applyOverlayerButton.Location = new System.Drawing.Point(16, 12);
			this.applyOverlayerButton.Name = "applyOverlayerButton";
			this.applyOverlayerButton.Size = new System.Drawing.Size(147, 23);
			this.applyOverlayerButton.TabIndex = 2;
			this.applyOverlayerButton.Text = "[A]pply Changes";
			this.applyOverlayerButton.UseVisualStyleBackColor = true;
			// 
			// overlayerTextBox
			// 
			this.overlayerTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.overlayerTextBox.ExtraDescent = 1;
			this.overlayerTextBox.Keywords0 = "";
			this.overlayerTextBox.Keywords1 = "";
			this.overlayerTextBox.KeywordsImplicit = "True False Some None Right Left";
			this.overlayerTextBox.Lexer = ScintillaNET.Lexer.Lisp;
			this.overlayerTextBox.Location = new System.Drawing.Point(0, 0);
			this.overlayerTextBox.Name = "overlayerTextBox";
			this.overlayerTextBox.Size = new System.Drawing.Size(770, 179);
			this.overlayerTextBox.TabIndex = 0;
			// 
			// evaluatorTabPage
			// 
			this.evaluatorTabPage.Controls.Add(this.terminal);
			this.evaluatorTabPage.Location = new System.Drawing.Point(4, 22);
			this.evaluatorTabPage.Name = "evaluatorTabPage";
			this.evaluatorTabPage.Size = new System.Drawing.Size(956, 185);
			this.evaluatorTabPage.TabIndex = 4;
			this.evaluatorTabPage.Text = "[E]valuator";
			this.evaluatorTabPage.UseVisualStyleBackColor = true;
			// 
			// terminal
			// 
			this.terminal.Dock = System.Windows.Forms.DockStyle.Fill;
			this.terminal.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
			this.terminal.Location = new System.Drawing.Point(0, 0);
			this.terminal.Name = "terminal";
			// 
			// terminal.Panel1
			// 
			this.terminal.Panel1.Controls.Add(this.evalInputTextBox);
			// 
			// terminal.Panel2
			// 
			this.terminal.Panel2.Controls.Add(this.splitContainer10);
			this.terminal.Size = new System.Drawing.Size(956, 185);
			this.terminal.SplitterDistance = 527;
			this.terminal.TabIndex = 0;
			// 
			// evalInputTextBox
			// 
			this.evalInputTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.evalInputTextBox.ExtraDescent = 1;
			this.evalInputTextBox.Keywords0 = "";
			this.evalInputTextBox.Keywords1 = "";
			this.evalInputTextBox.KeywordsImplicit = "True False Some None Right Left";
			this.evalInputTextBox.Lexer = ScintillaNET.Lexer.Lisp;
			this.evalInputTextBox.Location = new System.Drawing.Point(0, 0);
			this.evalInputTextBox.Name = "evalInputTextBox";
			this.evalInputTextBox.Size = new System.Drawing.Size(527, 185);
			this.evalInputTextBox.TabIndex = 0;
			this.evalInputTextBox.Text = "; Evaluate script expressions here!\n[+ 2 2]\n";
			// 
			// splitContainer10
			// 
			this.splitContainer10.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer10.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.splitContainer10.IsSplitterFixed = true;
			this.splitContainer10.Location = new System.Drawing.Point(0, 0);
			this.splitContainer10.Name = "splitContainer10";
			// 
			// splitContainer10.Panel1
			// 
			this.splitContainer10.Panel1.Controls.Add(this.evalLineButton);
			this.splitContainer10.Panel1.Controls.Add(this.clearOutputButton);
			this.splitContainer10.Panel1.Controls.Add(this.evalButton);
			// 
			// splitContainer10.Panel2
			// 
			this.splitContainer10.Panel2.Controls.Add(this.evalOutputTextBox);
			this.splitContainer10.Size = new System.Drawing.Size(425, 185);
			this.splitContainer10.SplitterDistance = 44;
			this.splitContainer10.TabIndex = 1;
			// 
			// evalLineButton
			// 
			this.evalLineButton.Enabled = false;
			this.evalLineButton.Location = new System.Drawing.Point(3, 60);
			this.evalLineButton.Name = "evalLineButton";
			this.evalLineButton.Size = new System.Drawing.Size(42, 55);
			this.evalLineButton.TabIndex = 2;
			this.evalLineButton.Text = "Eval [L]ine";
			this.evalLineButton.UseVisualStyleBackColor = true;
			// 
			// clearOutputButton
			// 
			this.clearOutputButton.Location = new System.Drawing.Point(3, 117);
			this.clearOutputButton.Name = "clearOutputButton";
			this.clearOutputButton.Size = new System.Drawing.Size(42, 54);
			this.clearOutputButton.TabIndex = 1;
			this.clearOutputButton.Text = "Clear";
			this.clearOutputButton.UseVisualStyleBackColor = true;
			// 
			// evalButton
			// 
			this.evalButton.Location = new System.Drawing.Point(3, 3);
			this.evalButton.Name = "evalButton";
			this.evalButton.Size = new System.Drawing.Size(42, 55);
			this.evalButton.TabIndex = 0;
			this.evalButton.Text = "E[v]al";
			this.evalButton.UseVisualStyleBackColor = true;
			// 
			// evalOutputTextBox
			// 
			this.evalOutputTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.evalOutputTextBox.ExtraDescent = 1;
			this.evalOutputTextBox.Keywords0 = "";
			this.evalOutputTextBox.Keywords1 = "";
			this.evalOutputTextBox.KeywordsImplicit = "True False Some None Right Left";
			this.evalOutputTextBox.Lexer = ScintillaNET.Lexer.Lisp;
			this.evalOutputTextBox.Location = new System.Drawing.Point(0, 0);
			this.evalOutputTextBox.Name = "evalOutputTextBox";
			this.evalOutputTextBox.ReadOnly = true;
			this.evalOutputTextBox.Size = new System.Drawing.Size(377, 185);
			this.evalOutputTextBox.TabIndex = 0;
			// 
			// preludeTabPage
			// 
			this.preludeTabPage.Controls.Add(this.splitContainer6);
			this.preludeTabPage.Location = new System.Drawing.Point(4, 22);
			this.preludeTabPage.Name = "preludeTabPage";
			this.preludeTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.preludeTabPage.Size = new System.Drawing.Size(956, 185);
			this.preludeTabPage.TabIndex = 5;
			this.preludeTabPage.Text = "Prelude";
			this.preludeTabPage.UseVisualStyleBackColor = true;
			// 
			// splitContainer6
			// 
			this.splitContainer6.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer6.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.splitContainer6.IsSplitterFixed = true;
			this.splitContainer6.Location = new System.Drawing.Point(3, 3);
			this.splitContainer6.Name = "splitContainer6";
			// 
			// splitContainer6.Panel1
			// 
			this.splitContainer6.Panel1.Controls.Add(this.discardPreludeButton);
			this.splitContainer6.Panel1.Controls.Add(this.applyPreludeButton);
			// 
			// splitContainer6.Panel2
			// 
			this.splitContainer6.Panel2.Controls.Add(this.preludeTextBox);
			this.splitContainer6.Size = new System.Drawing.Size(950, 179);
			this.splitContainer6.SplitterDistance = 176;
			this.splitContainer6.TabIndex = 3;
			// 
			// discardPreludeButton
			// 
			this.discardPreludeButton.Location = new System.Drawing.Point(16, 41);
			this.discardPreludeButton.Name = "discardPreludeButton";
			this.discardPreludeButton.Size = new System.Drawing.Size(147, 23);
			this.discardPreludeButton.TabIndex = 4;
			this.discardPreludeButton.Text = "[D]iscard Changes";
			this.discardPreludeButton.UseVisualStyleBackColor = true;
			// 
			// applyPreludeButton
			// 
			this.applyPreludeButton.Location = new System.Drawing.Point(16, 12);
			this.applyPreludeButton.Name = "applyPreludeButton";
			this.applyPreludeButton.Size = new System.Drawing.Size(147, 23);
			this.applyPreludeButton.TabIndex = 2;
			this.applyPreludeButton.Text = "[A]pply Changes";
			this.applyPreludeButton.UseVisualStyleBackColor = true;
			// 
			// preludeTextBox
			// 
			this.preludeTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.preludeTextBox.ExtraDescent = 1;
			this.preludeTextBox.Keywords0 = "";
			this.preludeTextBox.Keywords1 = "";
			this.preludeTextBox.KeywordsImplicit = "True False Some None Right Left";
			this.preludeTextBox.Lexer = ScintillaNET.Lexer.Lisp;
			this.preludeTextBox.Location = new System.Drawing.Point(0, 0);
			this.preludeTextBox.Name = "preludeTextBox";
			this.preludeTextBox.Size = new System.Drawing.Size(770, 179);
			this.preludeTextBox.TabIndex = 0;
			// 
			// eventTracingTabPage
			// 
			this.eventTracingTabPage.Controls.Add(this.eventTracing);
			this.eventTracingTabPage.Location = new System.Drawing.Point(4, 22);
			this.eventTracingTabPage.Name = "eventTracingTabPage";
			this.eventTracingTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.eventTracingTabPage.Size = new System.Drawing.Size(956, 185);
			this.eventTracingTabPage.TabIndex = 1;
			this.eventTracingTabPage.Text = "Event Tracing";
			this.eventTracingTabPage.UseVisualStyleBackColor = true;
			// 
			// eventTracing
			// 
			this.eventTracing.Dock = System.Windows.Forms.DockStyle.Fill;
			this.eventTracing.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.eventTracing.IsSplitterFixed = true;
			this.eventTracing.Location = new System.Drawing.Point(3, 3);
			this.eventTracing.Name = "eventTracing";
			// 
			// eventTracing.Panel1
			// 
			this.eventTracing.Panel1.Controls.Add(this.discardEventFilterButton);
			this.eventTracing.Panel1.Controls.Add(this.traceEventsCheckBox);
			this.eventTracing.Panel1.Controls.Add(this.applyEventFilterButton);
			// 
			// eventTracing.Panel2
			// 
			this.eventTracing.Panel2.Controls.Add(this.eventFilterTextBox);
			this.eventTracing.Size = new System.Drawing.Size(950, 179);
			this.eventTracing.SplitterDistance = 176;
			this.eventTracing.TabIndex = 0;
			// 
			// discardEventFilterButton
			// 
			this.discardEventFilterButton.Location = new System.Drawing.Point(16, 64);
			this.discardEventFilterButton.Name = "discardEventFilterButton";
			this.discardEventFilterButton.Size = new System.Drawing.Size(147, 23);
			this.discardEventFilterButton.TabIndex = 4;
			this.discardEventFilterButton.Text = "[D]iscard Changes";
			this.discardEventFilterButton.UseVisualStyleBackColor = true;
			// 
			// traceEventsCheckBox
			// 
			this.traceEventsCheckBox.AutoSize = true;
			this.traceEventsCheckBox.Location = new System.Drawing.Point(16, 12);
			this.traceEventsCheckBox.Name = "traceEventsCheckBox";
			this.traceEventsCheckBox.Size = new System.Drawing.Size(96, 17);
			this.traceEventsCheckBox.TabIndex = 3;
			this.traceEventsCheckBox.Text = "[T]race Events";
			this.traceEventsCheckBox.UseVisualStyleBackColor = true;
			// 
			// applyEventFilterButton
			// 
			this.applyEventFilterButton.Location = new System.Drawing.Point(16, 35);
			this.applyEventFilterButton.Name = "applyEventFilterButton";
			this.applyEventFilterButton.Size = new System.Drawing.Size(147, 23);
			this.applyEventFilterButton.TabIndex = 2;
			this.applyEventFilterButton.Text = "[A]pply Changes";
			this.applyEventFilterButton.UseVisualStyleBackColor = true;
			// 
			// eventFilterTextBox
			// 
			this.eventFilterTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.eventFilterTextBox.ExtraDescent = 1;
			this.eventFilterTextBox.Keywords0 = "";
			this.eventFilterTextBox.Keywords1 = "";
			this.eventFilterTextBox.KeywordsImplicit = "True False Some None Right Left";
			this.eventFilterTextBox.Lexer = ScintillaNET.Lexer.Lisp;
			this.eventFilterTextBox.Location = new System.Drawing.Point(0, 0);
			this.eventFilterTextBox.Name = "eventFilterTextBox";
			this.eventFilterTextBox.Size = new System.Drawing.Size(770, 179);
			this.eventFilterTextBox.TabIndex = 0;
			// 
			// propertyTabControl
			// 
			this.propertyTabControl.Controls.Add(this.entityTabPage);
			this.propertyTabControl.Controls.Add(this.groupTabPage);
			this.propertyTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.propertyTabControl.Location = new System.Drawing.Point(0, 0);
			this.propertyTabControl.Name = "propertyTabControl";
			this.propertyTabControl.SelectedIndex = 0;
			this.propertyTabControl.Size = new System.Drawing.Size(309, 669);
			this.propertyTabControl.TabIndex = 1;
			// 
			// entityTabPage
			// 
			this.entityTabPage.Controls.Add(this.splitContainer7);
			this.entityTabPage.Location = new System.Drawing.Point(4, 22);
			this.entityTabPage.Name = "entityTabPage";
			this.entityTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.entityTabPage.Size = new System.Drawing.Size(301, 643);
			this.entityTabPage.TabIndex = 0;
			this.entityTabPage.Text = "Entity Properties";
			this.entityTabPage.UseVisualStyleBackColor = true;
			// 
			// splitContainer7
			// 
			this.splitContainer7.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer7.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.splitContainer7.IsSplitterFixed = true;
			this.splitContainer7.Location = new System.Drawing.Point(3, 3);
			this.splitContainer7.Name = "splitContainer7";
			this.splitContainer7.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer7.Panel1
			// 
			this.splitContainer7.Panel1.Controls.Add(this.entityPropertyDesigner);
			// 
			// splitContainer7.Panel2
			// 
			this.splitContainer7.Panel2.Controls.Add(this.panel1);
			this.splitContainer7.Size = new System.Drawing.Size(295, 637);
			this.splitContainer7.SplitterDistance = 84;
			this.splitContainer7.TabIndex = 1;
			// 
			// entityPropertyDesigner
			// 
			this.entityPropertyDesigner.Controls.Add(this.entityIgnorePropertyBindingsCheckBox);
			this.entityPropertyDesigner.Controls.Add(this.entityDesignerPropertyRemoveButton);
			this.entityPropertyDesigner.Controls.Add(this.entityDesignerPropertyNameTextBox);
			this.entityPropertyDesigner.Controls.Add(this.label2);
			this.entityPropertyDesigner.Controls.Add(this.entityDesignerPropertyTypeComboBox);
			this.entityPropertyDesigner.Controls.Add(this.label1);
			this.entityPropertyDesigner.Controls.Add(this.entityDesignerPropertyDefaultButton);
			this.entityPropertyDesigner.Controls.Add(this.entityDesignerPropertyAddButton);
			this.entityPropertyDesigner.Dock = System.Windows.Forms.DockStyle.Fill;
			this.entityPropertyDesigner.Location = new System.Drawing.Point(0, 0);
			this.entityPropertyDesigner.Name = "entityPropertyDesigner";
			this.entityPropertyDesigner.Size = new System.Drawing.Size(295, 84);
			this.entityPropertyDesigner.TabIndex = 0;
			this.entityPropertyDesigner.TabStop = false;
			// 
			// entityIgnorePropertyBindingsCheckBox
			// 
			this.entityIgnorePropertyBindingsCheckBox.AutoSize = true;
			this.entityIgnorePropertyBindingsCheckBox.Location = new System.Drawing.Point(6, 60);
			this.entityIgnorePropertyBindingsCheckBox.Name = "entityIgnorePropertyBindingsCheckBox";
			this.entityIgnorePropertyBindingsCheckBox.Size = new System.Drawing.Size(211, 17);
			this.entityIgnorePropertyBindingsCheckBox.TabIndex = 5;
			this.entityIgnorePropertyBindingsCheckBox.Text = "Ignore Property Bindings (Session Only)";
			this.entityIgnorePropertyBindingsCheckBox.UseVisualStyleBackColor = true;
			// 
			// entityDesignerPropertyRemoveButton
			// 
			this.entityDesignerPropertyRemoveButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.entityDesignerPropertyRemoveButton.Location = new System.Drawing.Point(277, 32);
			this.entityDesignerPropertyRemoveButton.Name = "entityDesignerPropertyRemoveButton";
			this.entityDesignerPropertyRemoveButton.Size = new System.Drawing.Size(17, 23);
			this.entityDesignerPropertyRemoveButton.TabIndex = 4;
			this.entityDesignerPropertyRemoveButton.Text = "-";
			this.entityDesignerPropertyRemoveButton.UseVisualStyleBackColor = true;
			// 
			// entityDesignerPropertyNameTextBox
			// 
			this.entityDesignerPropertyNameTextBox.Location = new System.Drawing.Point(5, 33);
			this.entityDesignerPropertyNameTextBox.Name = "entityDesignerPropertyNameTextBox";
			this.entityDesignerPropertyNameTextBox.Size = new System.Drawing.Size(113, 20);
			this.entityDesignerPropertyNameTextBox.TabIndex = 0;
			this.entityDesignerPropertyNameTextBox.Text = "MyProperty";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(117, 16);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(34, 13);
			this.label2.TabIndex = 2;
			this.label2.Text = "Value";
			// 
			// entityDesignerPropertyTypeComboBox
			// 
			this.entityDesignerPropertyTypeComboBox.DropDownWidth = 152;
			this.entityDesignerPropertyTypeComboBox.FormattingEnabled = true;
			this.entityDesignerPropertyTypeComboBox.Location = new System.Drawing.Point(120, 33);
			this.entityDesignerPropertyTypeComboBox.Name = "entityDesignerPropertyTypeComboBox";
			this.entityDesignerPropertyTypeComboBox.Size = new System.Drawing.Size(117, 21);
			this.entityDesignerPropertyTypeComboBox.TabIndex = 1;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(3, 16);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(91, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Designer Property";
			// 
			// entityDesignerPropertyDefaultButton
			// 
			this.entityDesignerPropertyDefaultButton.Location = new System.Drawing.Point(259, 32);
			this.entityDesignerPropertyDefaultButton.Name = "entityDesignerPropertyDefaultButton";
			this.entityDesignerPropertyDefaultButton.Size = new System.Drawing.Size(17, 23);
			this.entityDesignerPropertyDefaultButton.TabIndex = 3;
			this.entityDesignerPropertyDefaultButton.Text = "0";
			this.entityDesignerPropertyDefaultButton.UseVisualStyleBackColor = true;
			// 
			// entityDesignerPropertyAddButton
			// 
			this.entityDesignerPropertyAddButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.entityDesignerPropertyAddButton.Location = new System.Drawing.Point(241, 32);
			this.entityDesignerPropertyAddButton.Name = "entityDesignerPropertyAddButton";
			this.entityDesignerPropertyAddButton.Size = new System.Drawing.Size(17, 23);
			this.entityDesignerPropertyAddButton.TabIndex = 2;
			this.entityDesignerPropertyAddButton.Text = "+";
			this.entityDesignerPropertyAddButton.UseVisualStyleBackColor = true;
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.entityPropertyGrid);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel1.Location = new System.Drawing.Point(0, 0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(295, 549);
			this.panel1.TabIndex = 2;
			// 
			// entityPropertyGrid
			// 
			this.entityPropertyGrid.CategoryForeColor = System.Drawing.SystemColors.InactiveCaptionText;
			this.entityPropertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.entityPropertyGrid.LineColor = System.Drawing.SystemColors.ControlDark;
			this.entityPropertyGrid.Location = new System.Drawing.Point(0, 0);
			this.entityPropertyGrid.Margin = new System.Windows.Forms.Padding(2);
			this.entityPropertyGrid.Name = "entityPropertyGrid";
			this.entityPropertyGrid.Size = new System.Drawing.Size(295, 549);
			this.entityPropertyGrid.TabIndex = 0;
			this.entityPropertyGrid.ToolbarVisible = false;
			// 
			// groupTabPage
			// 
			this.groupTabPage.Controls.Add(this.groupPropertyGrid);
			this.groupTabPage.Location = new System.Drawing.Point(4, 22);
			this.groupTabPage.Name = "groupTabPage";
			this.groupTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.groupTabPage.Size = new System.Drawing.Size(301, 643);
			this.groupTabPage.TabIndex = 1;
			this.groupTabPage.Text = "Group Properties";
			this.groupTabPage.UseVisualStyleBackColor = true;
			// 
			// groupPropertyGrid
			// 
			this.groupPropertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.groupPropertyGrid.LineColor = System.Drawing.SystemColors.ControlDark;
			this.groupPropertyGrid.Location = new System.Drawing.Point(3, 3);
			this.groupPropertyGrid.Name = "groupPropertyGrid";
			this.groupPropertyGrid.Size = new System.Drawing.Size(295, 637);
			this.groupPropertyGrid.TabIndex = 0;
			this.groupPropertyGrid.ToolbarVisible = false;
			// 
			// openFileDialog
			// 
			this.openFileDialog.DefaultExt = "nugroup";
			this.openFileDialog.Filter = "Nu Groups|*.nugroup|All files|*.*";
			// 
			// saveFileDialog
			// 
			this.saveFileDialog.DefaultExt = "nugroup";
			this.saveFileDialog.Filter = "Nu Groups|*.nugroup|All files|*.*";
			// 
			// undoButton
			// 
			this.undoButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.undoButton.Image = ((System.Drawing.Image)(resources.GetObject("undoButton.Image")));
			this.undoButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.undoButton.Name = "undoButton";
			this.undoButton.Size = new System.Drawing.Size(40, 22);
			this.undoButton.Text = "Undo";
			// 
			// redoButton
			// 
			this.redoButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.redoButton.Image = ((System.Drawing.Image)(resources.GetObject("redoButton.Image")));
			this.redoButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.redoButton.Name = "redoButton";
			this.redoButton.Size = new System.Drawing.Size(38, 22);
			this.redoButton.Text = "Redo";
			// 
			// toolStripSeparator6
			// 
			this.toolStripSeparator6.Name = "toolStripSeparator6";
			this.toolStripSeparator6.Size = new System.Drawing.Size(6, 25);
			// 
			// positionSnapLabel
			// 
			this.positionSnapLabel.Name = "positionSnapLabel";
			this.positionSnapLabel.Size = new System.Drawing.Size(58, 22);
			this.positionSnapLabel.Text = "Pos. Snap";
			// 
			// positionSnapTextBox
			// 
			this.positionSnapTextBox.Font = new System.Drawing.Font("Tahoma", 8.25F);
			this.positionSnapTextBox.Name = "positionSnapTextBox";
			this.positionSnapTextBox.Size = new System.Drawing.Size(20, 25);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
			// 
			// rotationSnapLabel
			// 
			this.rotationSnapLabel.Name = "rotationSnapLabel";
			this.rotationSnapLabel.Size = new System.Drawing.Size(57, 22);
			this.rotationSnapLabel.Text = "Rot. Snap";
			// 
			// rotationSnapTextBox
			// 
			this.rotationSnapTextBox.Font = new System.Drawing.Font("Tahoma", 8.25F);
			this.rotationSnapTextBox.Name = "rotationSnapTextBox";
			this.rotationSnapTextBox.Size = new System.Drawing.Size(20, 25);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
			// 
			// createEntityButton
			// 
			this.createEntityButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.createEntityButton.Image = ((System.Drawing.Image)(resources.GetObject("createEntityButton.Image")));
			this.createEntityButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.createEntityButton.Name = "createEntityButton";
			this.createEntityButton.Size = new System.Drawing.Size(45, 22);
			this.createEntityButton.Text = "Create";
			// 
			// createEntityComboBox
			// 
			this.createEntityComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.createEntityComboBox.Font = new System.Drawing.Font("Tahoma", 8.25F);
			this.createEntityComboBox.Name = "createEntityComboBox";
			this.createEntityComboBox.Size = new System.Drawing.Size(114, 25);
			// 
			// toolStripLabel1
			// 
			this.toolStripLabel1.Name = "toolStripLabel1";
			this.toolStripLabel1.Size = new System.Drawing.Size(21, 22);
			this.toolStripLabel1.Text = "w/";
			// 
			// overlayComboBox
			// 
			this.overlayComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.overlayComboBox.Font = new System.Drawing.Font("Tahoma", 8.25F);
			this.overlayComboBox.Name = "overlayComboBox";
			this.overlayComboBox.Size = new System.Drawing.Size(114, 25);
			// 
			// creationElevationLabel
			// 
			this.creationElevationLabel.Name = "creationElevationLabel";
			this.creationElevationLabel.Size = new System.Drawing.Size(69, 22);
			this.creationElevationLabel.Text = "@ elevation";
			// 
			// createElevationMinusButton
			// 
			this.createElevationMinusButton.AutoSize = false;
			this.createElevationMinusButton.AutoToolTip = false;
			this.createElevationMinusButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.createElevationMinusButton.Image = ((System.Drawing.Image)(resources.GetObject("createElevationMinusButton.Image")));
			this.createElevationMinusButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.createElevationMinusButton.Name = "createElevationMinusButton";
			this.createElevationMinusButton.Size = new System.Drawing.Size(17, 22);
			this.createElevationMinusButton.Text = "[-";
			// 
			// createElevationTextBox
			// 
			this.createElevationTextBox.Font = new System.Drawing.Font("Tahoma", 8.25F);
			this.createElevationTextBox.Name = "createElevationTextBox";
			this.createElevationTextBox.Size = new System.Drawing.Size(20, 25);
			// 
			// createElevationPlusButton
			// 
			this.createElevationPlusButton.AutoSize = false;
			this.createElevationPlusButton.AutoToolTip = false;
			this.createElevationPlusButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.createElevationPlusButton.Image = ((System.Drawing.Image)(resources.GetObject("createElevationPlusButton.Image")));
			this.createElevationPlusButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.createElevationPlusButton.Name = "createElevationPlusButton";
			this.createElevationPlusButton.Size = new System.Drawing.Size(17, 22);
			this.createElevationPlusButton.Text = "+]";
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
			// 
			// quickSizeToolStripButton
			// 
			this.quickSizeToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.quickSizeToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("quickSizeToolStripButton.Image")));
			this.quickSizeToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.quickSizeToolStripButton.Name = "quickSizeToolStripButton";
			this.quickSizeToolStripButton.Size = new System.Drawing.Size(65, 22);
			this.quickSizeToolStripButton.Text = "Quick Size";
			// 
			// toolStripSeparator4
			// 
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			this.toolStripSeparator4.Size = new System.Drawing.Size(6, 25);
			// 
			// deleteEntityButton
			// 
			this.deleteEntityButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.deleteEntityButton.Image = ((System.Drawing.Image)(resources.GetObject("deleteEntityButton.Image")));
			this.deleteEntityButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.deleteEntityButton.Name = "deleteEntityButton";
			this.deleteEntityButton.Size = new System.Drawing.Size(44, 22);
			this.deleteEntityButton.Text = "Delete";
			// 
			// toolStripSeparator5
			// 
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			this.toolStripSeparator5.Size = new System.Drawing.Size(6, 25);
			// 
			// resetCameraButton
			// 
			this.resetCameraButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.resetCameraButton.Image = ((System.Drawing.Image)(resources.GetObject("resetCameraButton.Image")));
			this.resetCameraButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.resetCameraButton.Name = "resetCameraButton";
			this.resetCameraButton.Size = new System.Drawing.Size(67, 22);
			this.resetCameraButton.Text = "Reset Cam";
			// 
			// toolStripSeparator8
			// 
			this.toolStripSeparator8.Name = "toolStripSeparator8";
			this.toolStripSeparator8.Size = new System.Drawing.Size(6, 25);
			// 
			// reloadAssetsButton
			// 
			this.reloadAssetsButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.reloadAssetsButton.Image = ((System.Drawing.Image)(resources.GetObject("reloadAssetsButton.Image")));
			this.reloadAssetsButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.reloadAssetsButton.Name = "reloadAssetsButton";
			this.reloadAssetsButton.Size = new System.Drawing.Size(83, 22);
			this.reloadAssetsButton.Text = "Reload Assets";
			// 
			// toolStripSeparator9
			// 
			this.toolStripSeparator9.Name = "toolStripSeparator9";
			this.toolStripSeparator9.Size = new System.Drawing.Size(6, 25);
			// 
			// advancingButton
			// 
			this.advancingButton.CheckOnClick = true;
			this.advancingButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.advancingButton.Image = ((System.Drawing.Image)(resources.GetObject("advancingButton.Image")));
			this.advancingButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.advancingButton.Name = "advancingButton";
			this.advancingButton.Size = new System.Drawing.Size(91, 22);
			this.advancingButton.Text = "Advancing (F5)";
			// 
			// editWhileInteractiveCheckBox
			// 
			this.editWhileInteractiveCheckBox.CheckOnClick = true;
			this.editWhileInteractiveCheckBox.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.editWhileInteractiveCheckBox.Image = ((System.Drawing.Image)(resources.GetObject("editWhileInteractiveCheckBox.Image")));
			this.editWhileInteractiveCheckBox.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.editWhileInteractiveCheckBox.Name = "editWhileInteractiveCheckBox";
			this.editWhileInteractiveCheckBox.Size = new System.Drawing.Size(64, 22);
			this.editWhileInteractiveCheckBox.Text = "Edit While";
			// 
			// toolStrip
			// 
			this.toolStrip.Dock = System.Windows.Forms.DockStyle.None;
			this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileDropDownButton,
            this.editDropDownButton,
            this.undoButton,
            this.redoButton,
            this.toolStripSeparator6,
            this.positionSnapLabel,
            this.positionSnapTextBox,
            this.toolStripSeparator1,
            this.rotationSnapLabel,
            this.rotationSnapTextBox,
            this.toolStripSeparator2,
            this.createEntityButton,
            this.createEntityComboBox,
            this.toolStripLabel1,
            this.overlayComboBox,
            this.creationElevationLabel,
            this.createElevationMinusButton,
            this.createElevationTextBox,
            this.createElevationPlusButton,
            this.toolStripSeparator3,
            this.quickSizeToolStripButton,
            this.toolStripSeparator4,
            this.deleteEntityButton,
            this.toolStripSeparator5,
            this.resetCameraButton,
            this.toolStripSeparator8,
            this.reloadAssetsButton,
            this.toolStripSeparator9,
            this.advancingButton,
            this.editWhileInteractiveCheckBox,
            this.toolStripSeparator7,
            this.songPlaybackButton});
			this.toolStrip.Location = new System.Drawing.Point(0, 0);
			this.toolStrip.Name = "toolStrip";
			this.toolStrip.Size = new System.Drawing.Size(1272, 25);
			this.toolStrip.TabIndex = 1;
			this.toolStrip.Text = "toolStrip1";
			// 
			// fileDropDownButton
			// 
			this.fileDropDownButton.AutoToolTip = false;
			this.fileDropDownButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.fileDropDownButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newGroupToolStripMenuItem,
            this.openGroupToolStripMenuItem,
            this.saveGroupToolStripMenuItem,
            this.saveGroupAsToolStripMenuItem,
            this.toolStripMenuItem7,
            this.clearGroupToolStripMenuItem,
            this.closeGroupToolStripMenuItem,
            this.toolStripMenuItem8,
            this.exitToolStripMenuItem});
			this.fileDropDownButton.Image = ((System.Drawing.Image)(resources.GetObject("fileDropDownButton.Image")));
			this.fileDropDownButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.fileDropDownButton.Name = "fileDropDownButton";
			this.fileDropDownButton.Size = new System.Drawing.Size(38, 22);
			this.fileDropDownButton.Text = "File";
			// 
			// newGroupToolStripMenuItem
			// 
			this.newGroupToolStripMenuItem.Name = "newGroupToolStripMenuItem";
			this.newGroupToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
			this.newGroupToolStripMenuItem.Text = "&New Group (Ctrl+N)";
			// 
			// openGroupToolStripMenuItem
			// 
			this.openGroupToolStripMenuItem.Name = "openGroupToolStripMenuItem";
			this.openGroupToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
			this.openGroupToolStripMenuItem.Text = "&Open Group (Ctrl+O)";
			// 
			// saveGroupToolStripMenuItem
			// 
			this.saveGroupToolStripMenuItem.Name = "saveGroupToolStripMenuItem";
			this.saveGroupToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
			this.saveGroupToolStripMenuItem.Text = "&Save Group (Ctrl+S)";
			// 
			// saveGroupAsToolStripMenuItem
			// 
			this.saveGroupAsToolStripMenuItem.Name = "saveGroupAsToolStripMenuItem";
			this.saveGroupAsToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
			this.saveGroupAsToolStripMenuItem.Text = "Save Group &As (Ctrl+A)";
			// 
			// toolStripMenuItem7
			// 
			this.toolStripMenuItem7.Name = "toolStripMenuItem7";
			this.toolStripMenuItem7.Size = new System.Drawing.Size(193, 6);
			// 
			// clearGroupToolStripMenuItem
			// 
			this.clearGroupToolStripMenuItem.Enabled = false;
			this.clearGroupToolStripMenuItem.Name = "clearGroupToolStripMenuItem";
			this.clearGroupToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
			this.clearGroupToolStripMenuItem.Text = "Clear Group";
			// 
			// closeGroupToolStripMenuItem
			// 
			this.closeGroupToolStripMenuItem.Name = "closeGroupToolStripMenuItem";
			this.closeGroupToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
			this.closeGroupToolStripMenuItem.Text = "Close Group";
			// 
			// toolStripMenuItem8
			// 
			this.toolStripMenuItem8.Name = "toolStripMenuItem8";
			this.toolStripMenuItem8.Size = new System.Drawing.Size(193, 6);
			// 
			// exitToolStripMenuItem
			// 
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
			this.exitToolStripMenuItem.Text = "E[&x]it";
			// 
			// editDropDownButton
			// 
			this.editDropDownButton.AutoToolTip = false;
			this.editDropDownButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.editDropDownButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.undoToolStripMenuItem,
            this.redoToolStripMenuItem,
            this.toolStripMenuItem9,
            this.cutToolStripMenuItem,
            this.copyToolStripMenuItem,
            this.pasteToolStripMenuItem,
            this.toolStripMenuItem10,
            this.createToolStripMenuItem,
            this.deleteToolStripMenuItem,
            this.quickSizeToolStripMenuItem,
            this.toolStripMenuItem11,
            this.startStopAdvancingToolStripMenuItem,
            this.toolStripMenuItem12,
            this.changeGroupNameToolStripMenuItem});
			this.editDropDownButton.Image = ((System.Drawing.Image)(resources.GetObject("editDropDownButton.Image")));
			this.editDropDownButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.editDropDownButton.Name = "editDropDownButton";
			this.editDropDownButton.Size = new System.Drawing.Size(40, 22);
			this.editDropDownButton.Text = "Edit";
			// 
			// undoToolStripMenuItem
			// 
			this.undoToolStripMenuItem.Name = "undoToolStripMenuItem";
			this.undoToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
			this.undoToolStripMenuItem.Text = "&Undo (Ctrl+Z)";
			// 
			// redoToolStripMenuItem
			// 
			this.redoToolStripMenuItem.Name = "redoToolStripMenuItem";
			this.redoToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
			this.redoToolStripMenuItem.Text = "&Redo (Ctrl+Y)";
			// 
			// toolStripMenuItem9
			// 
			this.toolStripMenuItem9.Name = "toolStripMenuItem9";
			this.toolStripMenuItem9.Size = new System.Drawing.Size(213, 6);
			// 
			// cutToolStripMenuItem
			// 
			this.cutToolStripMenuItem.Name = "cutToolStripMenuItem";
			this.cutToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
			this.cutToolStripMenuItem.Text = "C&ut (Ctrl+X)";
			// 
			// copyToolStripMenuItem
			// 
			this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
			this.copyToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
			this.copyToolStripMenuItem.Text = "&Copy (Ctrl+C)";
			// 
			// pasteToolStripMenuItem
			// 
			this.pasteToolStripMenuItem.Name = "pasteToolStripMenuItem";
			this.pasteToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
			this.pasteToolStripMenuItem.Text = "&Paste (Ctrl+V)";
			// 
			// toolStripMenuItem10
			// 
			this.toolStripMenuItem10.Name = "toolStripMenuItem10";
			this.toolStripMenuItem10.Size = new System.Drawing.Size(213, 6);
			// 
			// createToolStripMenuItem
			// 
			this.createToolStripMenuItem.Name = "createToolStripMenuItem";
			this.createToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
			this.createToolStripMenuItem.Text = "Cr&eate (Ctrl+E)";
			// 
			// deleteToolStripMenuItem
			// 
			this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
			this.deleteToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
			this.deleteToolStripMenuItem.Text = "&Delete (Ctrl+D)";
			// 
			// quickSizeToolStripMenuItem
			// 
			this.quickSizeToolStripMenuItem.Name = "quickSizeToolStripMenuItem";
			this.quickSizeToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
			this.quickSizeToolStripMenuItem.Text = "&Quick Size (Ctrl+Q)";
			// 
			// toolStripMenuItem11
			// 
			this.toolStripMenuItem11.Name = "toolStripMenuItem11";
			this.toolStripMenuItem11.Size = new System.Drawing.Size(213, 6);
			// 
			// startStopAdvancingToolStripMenuItem
			// 
			this.startStopAdvancingToolStripMenuItem.Name = "startStopAdvancingToolStripMenuItem";
			this.startStopAdvancingToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
			this.startStopAdvancingToolStripMenuItem.Text = "&Start / Stop Advancing (F5)";
			// 
			// toolStripMenuItem12
			// 
			this.toolStripMenuItem12.Name = "toolStripMenuItem12";
			this.toolStripMenuItem12.Size = new System.Drawing.Size(213, 6);
			// 
			// changeGroupNameToolStripMenuItem
			// 
			this.changeGroupNameToolStripMenuItem.Enabled = false;
			this.changeGroupNameToolStripMenuItem.Name = "changeGroupNameToolStripMenuItem";
			this.changeGroupNameToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
			this.changeGroupNameToolStripMenuItem.Text = "Change Group Name";
			// 
			// toolStripSeparator7
			// 
			this.toolStripSeparator7.Name = "toolStripSeparator7";
			this.toolStripSeparator7.Size = new System.Drawing.Size(6, 25);
			// 
			// songPlaybackButton
			// 
			this.songPlaybackButton.CheckOnClick = true;
			this.songPlaybackButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.songPlaybackButton.Image = ((System.Drawing.Image)(resources.GetObject("songPlaybackButton.Image")));
			this.songPlaybackButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.songPlaybackButton.Name = "songPlaybackButton";
			this.songPlaybackButton.Size = new System.Drawing.Size(23, 22);
			this.songPlaybackButton.Text = "toolStripButton1";
			this.songPlaybackButton.ToolTipText = "Toggle Song Playback";
			// 
			// GaiaForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1276, 693);
			this.Controls.Add(this.splitContainer1);
			this.Controls.Add(this.toolStrip);
			this.Controls.Add(this.menuStrip);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.MainMenuStrip = this.menuStrip;
			this.Margin = new System.Windows.Forms.Padding(2);
			this.Name = "GaiaForm";
			this.Text = "Gaia for Nu Game Engine";
			this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
			this.contextMenuStrip.ResumeLayout(false);
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			this.splitContainer4.Panel1.ResumeLayout(false);
			this.splitContainer4.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer4)).EndInit();
			this.splitContainer4.ResumeLayout(false);
			this.splitContainer8.Panel1.ResumeLayout(false);
			this.splitContainer8.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer8)).EndInit();
			this.splitContainer8.ResumeLayout(false);
			this.tabControl1.ResumeLayout(false);
			this.tabPage3.ResumeLayout(false);
			this.splitContainer9.Panel1.ResumeLayout(false);
			this.splitContainer9.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer9)).EndInit();
			this.splitContainer9.ResumeLayout(false);
			this.groupTabControl.ResumeLayout(false);
			this.rolloutTabControl.ResumeLayout(false);
			this.propertyEditorTabPage.ResumeLayout(false);
			this.propertyEditor.Panel1.ResumeLayout(false);
			this.propertyEditor.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.propertyEditor)).EndInit();
			this.propertyEditor.ResumeLayout(false);
			this.splitContainer5.Panel1.ResumeLayout(false);
			this.splitContainer5.Panel1.PerformLayout();
			this.splitContainer5.Panel2.ResumeLayout(false);
			this.splitContainer5.Panel2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer5)).EndInit();
			this.splitContainer5.ResumeLayout(false);
			this.assetGraphTabPage.ResumeLayout(false);
			this.assetGraph.Panel1.ResumeLayout(false);
			this.assetGraph.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.assetGraph)).EndInit();
			this.assetGraph.ResumeLayout(false);
			this.overlayTabPage.ResumeLayout(false);
			this.overlayer.Panel1.ResumeLayout(false);
			this.overlayer.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.overlayer)).EndInit();
			this.overlayer.ResumeLayout(false);
			this.evaluatorTabPage.ResumeLayout(false);
			this.terminal.Panel1.ResumeLayout(false);
			this.terminal.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.terminal)).EndInit();
			this.terminal.ResumeLayout(false);
			this.splitContainer10.Panel1.ResumeLayout(false);
			this.splitContainer10.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer10)).EndInit();
			this.splitContainer10.ResumeLayout(false);
			this.preludeTabPage.ResumeLayout(false);
			this.splitContainer6.Panel1.ResumeLayout(false);
			this.splitContainer6.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer6)).EndInit();
			this.splitContainer6.ResumeLayout(false);
			this.eventTracingTabPage.ResumeLayout(false);
			this.eventTracing.Panel1.ResumeLayout(false);
			this.eventTracing.Panel1.PerformLayout();
			this.eventTracing.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.eventTracing)).EndInit();
			this.eventTracing.ResumeLayout(false);
			this.propertyTabControl.ResumeLayout(false);
			this.entityTabPage.ResumeLayout(false);
			this.splitContainer7.Panel1.ResumeLayout(false);
			this.splitContainer7.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer7)).EndInit();
			this.splitContainer7.ResumeLayout(false);
			this.entityPropertyDesigner.ResumeLayout(false);
			this.entityPropertyDesigner.PerformLayout();
			this.panel1.ResumeLayout(false);
			this.groupTabPage.ResumeLayout(false);
			this.toolStrip.ResumeLayout(false);
			this.toolStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.MenuStrip menuStrip;
        public Nu.Gaia.Design.SelectablePanel displayPanel;
        public System.Windows.Forms.OpenFileDialog openFileDialog;
        public System.Windows.Forms.SaveFileDialog saveFileDialog;
        public System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        public System.Windows.Forms.ToolStripMenuItem copyContextMenuItem;
        public System.Windows.Forms.ToolStripMenuItem pasteContextMenuItem;
        public System.Windows.Forms.ToolStripMenuItem cutContextMenuItem;
        public System.Windows.Forms.ToolStripMenuItem deleteContextMenuItem;
        public System.Windows.Forms.ToolStripMenuItem createContextMenuItem;
        public System.Windows.Forms.ToolStripSeparator toolStripMenuItem3;
        public System.Windows.Forms.SplitContainer splitContainer1;
        public System.Windows.Forms.PropertyGrid entityPropertyGrid;
        private System.Windows.Forms.TabPage tabPage;
        public System.Windows.Forms.TabControl groupTabControl;
        private System.Windows.Forms.SplitContainer splitContainer4;
        public System.Windows.Forms.SplitContainer propertyEditor;
        private System.Windows.Forms.SplitContainer splitContainer5;
        public System.Windows.Forms.TextBox propertyDescriptionTextBox;
        public System.Windows.Forms.Label propertyNameLabel;
        public System.Windows.Forms.Button applyPropertyButton;
        public System.Windows.Forms.Button discardPropertyButton;
        public System.Windows.Forms.Button applyEventFilterButton;
        public System.Windows.Forms.CheckBox traceEventsCheckBox;
        public System.Windows.Forms.Button discardEventFilterButton;
        public System.Windows.Forms.TabPage propertyEditorTabPage;
        public System.Windows.Forms.TabPage eventTracingTabPage;
        public System.Windows.Forms.TabControl rolloutTabControl;
        public SymbolicTextBox propertyValueTextBox;
        public SymbolicTextBox eventFilterTextBox;
        public System.Windows.Forms.TabPage overlayTabPage;
        public System.Windows.Forms.Button discardAssetGraphButton;
        public System.Windows.Forms.Button applyAssetGraphButton;
        public SymbolicTextBox assetGraphTextBox;
        public System.Windows.Forms.Button discardOverlayerButton;
        public System.Windows.Forms.Button applyOverlayerButton;
        public SymbolicTextBox overlayerTextBox;
        public System.Windows.Forms.TabPage assetGraphTabPage;
        public SymbolicTextBox evalInputTextBox;
        public SymbolicTextBox evalOutputTextBox;
        private System.Windows.Forms.SplitContainer splitContainer10;
        public System.Windows.Forms.SplitContainer terminal;
        public System.Windows.Forms.SplitContainer eventTracing;
        public System.Windows.Forms.SplitContainer assetGraph;
        public System.Windows.Forms.SplitContainer overlayer;
        public System.Windows.Forms.Button evalButton;
        public System.Windows.Forms.Button clearOutputButton;
        public System.Windows.Forms.Button evalLineButton;
        public System.Windows.Forms.SplitContainer splitContainer6;
        public System.Windows.Forms.Button discardPreludeButton;
        public System.Windows.Forms.Button applyPreludeButton;
        public SymbolicTextBox preludeTextBox;
        public System.Windows.Forms.TabControl propertyTabControl;
        public System.Windows.Forms.PropertyGrid groupPropertyGrid;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage3;
        public System.Windows.Forms.TreeView hierarchyTreeView;
        private System.Windows.Forms.SplitContainer splitContainer7;
        private System.Windows.Forms.Panel panel1;
        public System.Windows.Forms.ComboBox entityDesignerPropertyTypeComboBox;
        public System.Windows.Forms.Button entityDesignerPropertyAddButton;
        public System.Windows.Forms.Button entityDesignerPropertyDefaultButton;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        public System.Windows.Forms.TextBox entityDesignerPropertyNameTextBox;
        public System.Windows.Forms.Button entityDesignerPropertyRemoveButton;
        public System.Windows.Forms.GroupBox entityPropertyDesigner;
        public System.Windows.Forms.Button pickPropertyButton;
        public System.Windows.Forms.TabPage entityTabPage;
        public System.Windows.Forms.TabPage groupTabPage;
        public System.Windows.Forms.ToolStripButton undoButton;
        public System.Windows.Forms.ToolStripButton redoButton;
        public System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
        public System.Windows.Forms.ToolStripLabel positionSnapLabel;
        public System.Windows.Forms.ToolStripTextBox positionSnapTextBox;
        public System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        public System.Windows.Forms.ToolStripLabel rotationSnapLabel;
        public System.Windows.Forms.ToolStripTextBox rotationSnapTextBox;
        public System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        public System.Windows.Forms.ToolStripButton createEntityButton;
        public System.Windows.Forms.ToolStripComboBox createEntityComboBox;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        public System.Windows.Forms.ToolStripComboBox overlayComboBox;
        public System.Windows.Forms.ToolStripLabel creationElevationLabel;
        public System.Windows.Forms.ToolStripButton createElevationMinusButton;
        public System.Windows.Forms.ToolStripTextBox createElevationTextBox;
        public System.Windows.Forms.ToolStripButton createElevationPlusButton;
        public System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        public System.Windows.Forms.ToolStripButton quickSizeToolStripButton;
        public System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        public System.Windows.Forms.ToolStripButton deleteEntityButton;
        public System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        public System.Windows.Forms.ToolStripButton resetCameraButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
        public System.Windows.Forms.ToolStripButton reloadAssetsButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator9;
        public System.Windows.Forms.ToolStripButton advancingButton;
        public System.Windows.Forms.ToolStripButton editWhileInteractiveCheckBox;
        public System.Windows.Forms.ToolStrip toolStrip;
        public System.Windows.Forms.ToolStripDropDownButton fileDropDownButton;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem7;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem8;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem9;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem10;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem11;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem12;
        public System.Windows.Forms.ToolStripDropDownButton editDropDownButton;
        public System.Windows.Forms.ToolStripMenuItem newGroupToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem openGroupToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem saveGroupToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem saveGroupAsToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem clearGroupToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem closeGroupToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem undoToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem redoToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem cutToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem pasteToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem createToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem quickSizeToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem startStopAdvancingToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem changeGroupNameToolStripMenuItem;
        public System.Windows.Forms.TabPage evaluatorTabPage;
        public System.Windows.Forms.TabPage preludeTabPage;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
        public System.Windows.Forms.ToolStripButton songPlaybackButton;
        public System.Windows.Forms.CheckBox entityIgnorePropertyBindingsCheckBox;
		private System.Windows.Forms.SplitContainer splitContainer8;
		private System.Windows.Forms.SplitContainer splitContainer9;
	}
}

