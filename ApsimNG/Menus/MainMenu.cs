﻿// -----------------------------------------------------------------------
// <copyright file="MainMenu.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Presenters
{
    using System;
    using System.Diagnostics;
    using Models.Core;

    /// <summary>
    /// This class contains methods for all main menu items that the ExplorerView exposes to the user.
    /// </summary>
    public class MainMenu
    {
        /// <summary>
        /// Reference to the ExplorerPresenter.
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainMenu" /> class.
        /// </summary>
        /// <param name="explorerPresenter">The explorer presenter to work with</param>
        public MainMenu(ExplorerPresenter explorerPresenter)
        {
            this.explorerPresenter = explorerPresenter;
        }

        /// <summary>
        /// User has clicked on Save
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [MainMenu(MenuName = "Save", Hotkey = "Ctrl+S")]
        public void OnSaveClick(object sender, EventArgs e)
        {
            this.explorerPresenter.Save();
        }

        /// <summary>
        /// User has clicked on SaveAs
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [MainMenu(MenuName = "Save As", Hotkey = "Ctrl+Shift+S")]
        public void OnSaveAsClick(object sender, EventArgs e)
        {
            this.explorerPresenter.SaveAs();
        }

        /// <summary>
        /// User has clicked on Undo
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [MainMenu(MenuName = "Undo", Hotkey = "Ctrl+Z")]
        public void OnUndoClick(object sender, EventArgs e)
        {
            this.explorerPresenter.CommandHistory.Undo();
        }

        /// <summary>
        /// User has clicked on Redo
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [MainMenu(MenuName = "Redo", Hotkey = "Ctrl+Y")]
        public void OnRedoClick(object sender, EventArgs e)
        {
            this.explorerPresenter.CommandHistory.Redo();
        }

        /// <summary>
        /// User has clicked on Redo
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [MainMenu(MenuName = "Split screen", Hotkey = "Ctrl+T")]
        public void ToggleSecondExplorerViewVisible(object sender, EventArgs e)
        {
            this.explorerPresenter.MainPresenter.ToggleSecondExplorerViewVisible();
        }

        /// <summary>
        /// User has clicked on clear status panel.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        [MainMenu(MenuName = "Clear Status", Hotkey = "Ctrl+G")]
        public void ClearStatusPanel(object sender, EventArgs args)
        {
            explorerPresenter.MainPresenter.ClearStatusPanel();
        }

        /// <summary>
        /// User has clicked on Help
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [MainMenu(MenuName = "Help", Hotkey = "Ctrl+H")]
        public void OnHelp(object sender, EventArgs e)
        {
            Process process = new Process();
            process.StartInfo.FileName = "https://apsimnextgeneration.netlify.com/";
            process.Start();
        }


    }
}