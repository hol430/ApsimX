// -----------------------------------------------------------------------
// <copyright file="MainMenuAttribute.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace Models.Core
{
    using System;

    /// <summary>
    /// Specifies that the related class should use the user interface view
    /// that has the specified name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class MainMenuAttribute : System.Attribute
    {
        /// <summary>
        /// Gets or sets the main menu name.
        /// </summary>
        public string MenuName { get; set; }

        /// <summary>
        /// Keyboard shortcut which can be used to activate the menu item.
        /// </summary>
        public string Hotkey { get; set; }
    } 
}
