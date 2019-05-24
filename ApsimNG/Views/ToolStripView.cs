// -----------------------------------------------------------------------
// <copyright file="ToolStripView.cs"  company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using Gtk;
    using Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    /// Encapsulates a toolstrip (button bar)
    /// </summary>
    public class ToolStripView : IToolStripView
    {
        private Toolbar toolStrip = null;

        private AccelGroup accelerators = new AccelGroup();

        /// <summary>Constructor</summary>
        public ToolStripView(Toolbar toolbar)
        {
            toolStrip = toolbar;
        }

        /// <summary>Destroy the toolstrip</summary>
        public void Destroy()
        {
            if (toolStrip?.Parent?.Parent is Notebook)
                (toolStrip.Parent.Parent as Notebook).SwitchPage -= OnSwitchPage;
            toolStrip.FocusInEvent -= OnFocusIn;
            toolStrip.FocusOutEvent -= OnFocusOut;
            foreach (Widget child in toolStrip.Children)
            {
                if (child is ToolButton)
                {
                    PropertyInfo pi = child.GetType().GetProperty("AfterSignals", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (pi != null)
                    {
                        System.Collections.Hashtable handlers = (System.Collections.Hashtable)pi.GetValue(child);
                        if (handlers != null && handlers.ContainsKey("clicked"))
                        {
                            EventHandler handler = (EventHandler)handlers["clicked"];
                            (child as ToolButton).Clicked -= handler;
                        }
                    }
                }
                toolStrip.Remove(child);
                child.Destroy();
            }
        }

        /// <summary>Populate the main menu tool strip.</summary>
        /// <param name="menuDescriptions">Descriptions for each item.</param>
        public void Populate(List<MenuDescriptionArgs> menuDescriptions)
        {
            foreach (Widget child in toolStrip.Children)
            {
                toolStrip.Remove(child);
                child.Destroy();
            }
            foreach (MenuDescriptionArgs description in menuDescriptions)
            {
                Gtk.Image image = null;
                Gdk.Pixbuf pixbuf = null;
                ManifestResourceInfo info = Assembly.GetExecutingAssembly().GetManifestResourceInfo(description.ResourceNameForImage);

                if (info != null)
                {
                    pixbuf = new Gdk.Pixbuf(null, description.ResourceNameForImage, 20, 20);
                    image = new Gtk.Image(pixbuf);
                }
                ToolItem item = new ToolItem();
                item.Expand = true;

                if (description.OnClick == null)
                {
                    Label toolbarlabel = new Label();
                    if (description.RightAligned)
                        toolbarlabel.Xalign = 1.0F;
                    toolbarlabel.Xpad = 10;
                    toolbarlabel.ModifyFg(StateType.Normal, new Gdk.Color(0x99, 0x99, 0x99));
                    toolbarlabel.Text = description.Name;
                    toolbarlabel.TooltipText = description.ToolTip;
                    toolbarlabel.Visible = !String.IsNullOrEmpty(toolbarlabel.Text);
                    item.Add(toolbarlabel);
                    toolStrip.Add(item);
                    toolStrip.ShowAll();
                }
                else
                {
                    ToolButton button = new ToolButton(image, description.Name);
                    if (!string.IsNullOrEmpty(description.ShortcutKey))
                    {
                        MenuView.GetAccelKey(description.ShortcutKey, out Gdk.Key key, out Gdk.ModifierType modifier);
                        button.AddAccelerator("clicked", accelerators, (uint)key, modifier, AccelFlags.Visible);
                    }

                    button.Homogeneous = false;
                    button.LabelWidget = new Label(description.Name);
                    button.Clicked += description.OnClick;
                    item = button;
                }
                toolStrip.Add(item);
            }
            toolStrip.ShowAll();
        }

        /// <summary>
        /// Intialises automatic adding/removal of hotkeys when a tab gains/loses focus.
        /// </summary>
        public void InitHotkeys()
        {
            if (toolStrip.Parent.Parent is Notebook)
            {
                (toolStrip.Parent.Parent as Notebook).SwitchPage += OnSwitchPage; // fixme
                toolStrip.FocusInEvent += OnFocusIn;
                toolStrip.FocusOutEvent += OnFocusOut;
            }
        }

        /// <summary>
        /// Registers the hotkeys by adding the accel group to the main window.
        /// </summary>
        private void AddHotkeys()
        {
            Console.WriteLine("Adding hotkeys.");
            if (toolStrip != null && toolStrip.Toplevel is Window)
                (toolStrip.Toplevel as Window).AddAccelGroup(accelerators);
        }

        /// <summary>
        /// Removes the hotkeys by removing the accel group from the main window.
        /// </summary>
        private void RemoveHotkeys()
        {
            Console.WriteLine("Removing hotkeys.");
            if (toolStrip != null && toolStrip.Toplevel is Window)
                (toolStrip.Toplevel as Window).RemoveAccelGroup(accelerators);
        }

        /// <summary>
        /// Invoked when the toolbar loses focus
        /// </summary>
        /// <param name="o">Sender object.</param>
        /// <param name="args">Event args.</param>
        [GLib.ConnectBefore]
        private void OnFocusOut(object o, FocusOutEventArgs args)
        {
            try
            {
                RemoveHotkeys();
            }
            catch //(Exception err)
            {
                //ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when the toolbar gains focus
        /// </summary>
        /// <param name="o">Sender object.</param>
        /// <param name="args">Event args.</param>
        [GLib.ConnectBefore]
        private void OnFocusIn(object sender, EventArgs e)
        {
            try
            {
                AddHotkeys();
            }
            catch //(Exception err)
            {
                //ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when tab is changed.
        /// </summary>
        /// <param name="o">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnSwitchPage(object o, SwitchPageArgs args)
        {
            try
            {
                if (toolStrip.Parent == null)
                    return;

                if ((toolStrip.Parent.Parent as Notebook).CurrentPageWidget == toolStrip.Parent)
                    AddHotkeys();
                else
                    RemoveHotkeys();
            }
            catch //(Exception err)
            {
                //ShowError(err);
            }
        }
    }
}
