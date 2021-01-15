﻿
namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.NetworkInformation;
    using System.Reflection;
    using System.Text;
    using APSIM.Shared.Utilities;
    using ApsimNG.Interfaces;
    using Models.CLEM;
    using Models.Core;
    using Views;

    /// <summary>
    /// Presenter to provide HTML description summary for CLEM models
    /// </summary>
    public class CLEMSummaryPresenter : IPresenter
    {
        /// <summary>
        /// The model
        /// </summary>
        private Model model;

        /// <summary>
        /// The view to use
        /// </summary>
        private IMarkdownView genericView;

        private ExplorerPresenter explorer;

        /// <summary>
        /// Attach the view
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="view">The view to attach</param>
        /// <param name="explorerPresenter">The explorer</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.model = model as Model;
            this.genericView = view as IMarkdownView; 
            explorer = explorerPresenter;

            // save summary to disk when component is first pressed regardless of user slecting summary tab as now goes to html in browser
            System.IO.File.WriteAllText(Path.Combine(Path.GetDirectoryName(explorer.ApsimXFile.FileName), "CurrentDescriptiveSummary.html"), CreateHTML(this.model));
        }

        public void Refresh()
        {
            this.genericView.Text = CreateMarkdown(this.model);

            // save summary to disk
            System.IO.File.WriteAllText(Path.Combine(Path.GetDirectoryName(explorer.ApsimXFile.FileName), "CurrentDescriptiveSummary.html"), CreateHTML(model));
        }

        public string CreateMarkdown(Model modelToSummarise)
        {
            // get help uri
            string helpURL = "";
            try
            {
                HelpUriAttribute helpAtt = ReflectionUtilities.GetAttribute(model.GetType(), typeof(HelpUriAttribute), false) as HelpUriAttribute;
                string modelHelpURL = "";
                if (helpAtt != null)
                {
                    modelHelpURL = helpAtt.ToString();
                }
                // does offline help exist
                var directory = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                string offlinePath = Path.Combine(directory, "CLEM/Help");
                if (File.Exists(Path.Combine(offlinePath, "Default.htm")))
                {
                    helpURL = "file:///" + offlinePath.Replace(@"\", "/") + "/" + modelHelpURL.TrimStart('/');
                }
                // is this application online for online help
                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    // set to web address
                    // not currently available during development until web help is launched
                    helpURL = "https://www.apsim.info/clem/" + modelHelpURL.TrimStart('/');
                }
                if (helpURL == "")
                {
                    helpURL = "https://www.apsim.info";
                }
                // auto update if setting set in system settings for CLEM
                //System.Diagnostics.Process.Start(helpURL);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            string summaryFilePath = Path.Combine(Path.GetDirectoryName(explorer.ApsimXFile.FileName), "CurrentDescriptiveSummary.html").Replace(@"\", "/");
            string markdownString = "";
            markdownString += $"[View descriptive summary of current settings in browser]({summaryFilePath} \"descriptive summary\")  \n  \n";
            markdownString += $"View reference details for this component [{modelToSummarise.GetType().Name}]({helpURL} \"{modelToSummarise.GetType().Name} help\")  \n";
            return markdownString;
        }

        public string CreateHTML(Model modelToSummarise) //void RefreshSummary()
        {
            // currently includes autoupdate script for display of summary information in browser
            // give APSIM Next Gen no longer has access to WebKit HTMLView in GTK for .Net core

            string htmlString = "<!DOCTYPE html>\n" +
                "<html>\n<head>\n<script type=\"text / javascript\" src=\"https://livejs.com/live.js\"></script>\n" +
                "<meta http-equiv=\"Cache-Control\" content=\"no-cache, no-store, must-revalidate\" />\n" +
                "<meta http-equiv = \"Pragma\" content = \"no-cache\" />\n" +
                "<meta http-equiv = \"Expires\" content = \"0\" />\n" +
                //"<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\" />\n" + 
                "<style>\n" +
                "body {color: [FontColor]; max-width:1000px; font-size:1em; font-family: Segoe UI, Arial, sans-serif}" + 
                "table {border-collapse: collapse; font-size:0.8em; }" +
                ".resource table,th,td {border: 1px solid #996633; }" +
                "table th {padding:8px; color:[HeaderFontColor];}" +
                "table td {padding:8px; }" +
                " td:nth-child(n+2) {text-align:center;}" +
                " th:nth-child(1) {text-align:left;}" +
                ".resource th {background-color: #996633 !important; }" +
                ".resource tr:nth-child(2n+3) {background:[ResRowBack] !important;}" +
                ".resource tr:nth-child(2n+2) {background:[ResRowBack2] !important;}" +
                ".resource td.fill {background-color: #c1946c !important;}" +
                ".resourceborder {border-color:#996633; border-width:1px; border-style:solid; padding:0px; background-color:Cornsilk !important; }" +
                ".resource h1,h2,h3 {color:#996633; } .activity h1,h2,h3 { color:#009999; margin-bottom:5px; }" +
                ".resourcebanner {background-color:#996633 !important; color:[ResFontBanner]; padding:5px; font-weight:bold; border-radius:5px 5px 0px 0px; }" +
                ".resourcebannerlight {background-color:#c1946c !important; color:[ResFontBanner]; border-radius:5px 5px 0px 0px; padding:5px 5px 5px 10px; margin-top:12px; font-weight:bold }" +
                ".resourcebannerdark {background-color:#996633 !important; color:[ResFontBanner]; border-radius:5px 5px 0px 0px; padding:5px 5px 5px 10px; margin-top:12px; font-weight:bold }" +
                ".resourcecontent {background-color:[ResContBack] !important; margin-bottom:40px; border-radius:0px 0px 5px 5px; border-color:#996633; border-width:1px; border-style:none solid solid solid; padding:10px;}" +
                ".resourcebanneralone {background-color:[ResContBack] !important; margin:10px 0px 5px 0px; border-radius:5px 5px 5px 5px; border-color:#996633; border-width:1px; border-style:solid solid solid solid; padding:5px;}" +
                ".resourcecontentlight {background-color:[ResContBackLight] !important; margin-bottom:10px; border-radius:0px 0px 5px 5px; border-color:#c1946c; border-width:0px 1px 1px 1px; border-style:none solid solid solid; padding:10px;}" +
                ".resourcecontentdark {background-color:[ResContBackDark] !important; margin-bottom:10px; border-radius:0px 0px 5px 5px; border-color:#996633; border-width:0px 1px 1px 1px; border-style:none solid solid solid; padding:10px;}" +
                ".resourcelink {color:#996633; font-weight:bold; background-color:Cornsilk !important; border-color:#996633; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; border-radius:3px; }" +
                ".activity th,td {padding:5px; }" +
                ".activity table,th,td {border: 1px solid #996633; }" +
                ".activity th {background-color: #996633 !important; }" +
                ".activity td.fill {background-color: #996633 !important; }" +
                ".activity table {border-collapse: collapse; font-size:0.8em; }" +
                ".activity h1 {color:#009999; } .activity h1,h2,h3 { color:#009999; margin-bottom:5px; }" +
                ".activityborder {border-color:#009999; border-width:2px; border-style:none none none solid; padding:0px 0px 0px 10px; margin-bottom:15px; }" +
                ".activityborderfull {border-color:#009999; border-radius:5px; background-color:#f0f0f0 !important; border-width:1px; border-style:solid; margin-bottom:40px; }" +
                ".activitybanner {background-color:#009999 !important; border-radius:5px 5px 0px 0px; color:#f0f0f0; padding:5px; font-weight:bold }" +
                ".activitybannerlight {background-color:#86b2b1 !important; border-radius:5px 5px 0px 0px; color:white; padding:5px 5px 5px 10px; margin-top:12px; font-weight:bold }" +
                ".activitybannerdark {background-color:#009999 !important; border-radius:5px 5px 0px 0px; color:white; padding:5px 5px 5px 10px; margin-top:12px; font-weight:bold }" +
                ".activitybannercontent {background-color:#86b2b1 !important; border-radius:5px 5px 0px 0px; padding:5px 5px 5px 10px; margin-top:5px; }" +
                ".activitycontent {background-color:[ActContBack] !important; margin-bottom:10px; border-radius:0px 0px 5px 5px; border-color:#009999; border-width:0px 1px 1px 1px; border-style:none solid solid solid; padding:10px;}" +
                ".activitycontentlight {background-color:[ActContBackLight] !important; margin-bottom:10px; border-radius:0px 0px 5px 5px; border-color:#86b2b1; border-width:0px 1px 1px 1px; border-style:solid; padding:10px;}" +
                ".activitycontentdark {background-color:[ActContBackDark] !important; margin-bottom:10px; border-radius:0px 0px 5px 5px; border-color:#86b2b1; border-width:0px 1px 1px 1px; border-style:none solid solid solid; padding:10px;}" +
                ".activitypadding {padding:10px; }" +
                ".activityentry {padding:5px 0px 5px 0px; }" +
                ".activityarea {padding:10px; }" +
                ".activitygroupsborder {border-color:#86b2b1; background-color:[ActContBackGroups] !important; border-width:1px; border-style:solid; padding:10px; margin-bottom:5px; margin-top:5px;}" +
                ".activitylink {color:#009999; font-weight:bold; background-color:[ActContBack] !important; border-color:#009999; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; border-radius:3px; }" +
                ".topspacing { margin-top:10px; }" +
                ".disabled { color:#CCC; }" +
                ".clearfix { overflow: auto; }" +
                ".namediv { float:left; vertical-align:middle; }" +
                ".typediv { float:right; vertical-align:middle; font-size:0.6em; }" +
                ".partialdiv { font-size:0.8em; float:right; text-transform: uppercase; color:white; font-weight:bold; vertical-align:middle; border-color:white; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; margin-left: 10px;  border-radius:3px; }" +
                ".filelink {color:green; font-weight:bold; background-color:mintcream !important; border-color:green; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; border-radius:3px; }" +
                ".errorlink {color:white; font-weight:bold; background-color:red !important; border-color:darkred; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; border-radius:3px; }" +
                ".setvalue {font-weight:bold; background-color: [ValueSetBack] !important; Color: [ValueSetFont]; border-color:#697c7c; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; border-radius:3px;}" +
                ".folder {color:#666666; font-style: italic; font-size:1.1em; }" +
                ".cropmixedlabel {color:#666666; font-style: italic; font-size:1.1em; padding: 5px 0px 10px 0px; }" +
                ".croprotationlabel {color:#666666; font-style: italic; font-size:1.1em; padding: 5px 0px 10px 0px; }" +
                ".cropmixedborder {border-color:#86b2b1; background-color:[CropRotationBack] !important; border-width:1px; border-style:solid; padding:0px 10px 0px 10px; margin-bottom:5px;margin-top:10px; }" +
                ".croprotationborder {border-color:#86b2b1; background-color:[CropRotationBack] !important; border-width:2px; border-style:solid; padding:0px 10px 0px 10px; margin-bottom:5px;margin-top:10px; }" +
                ".labourgroupsborder {border-color:[LabourGroupBorder]; background-color:[LabourGroupBack] !important; border-width:1px; border-style:solid; padding:10px; margin-bottom:5px; margin-top:5px;}" +
                ".labournote {font-style: italic; color:#666666; padding-top:7px;}" +
                ".warningbanner {background-color:Orange !important; border-radius:5px 5px 5px 5px; color:Black; padding:5px; font-weight:bold; margin-bottom:10px;margin-top:10px; }" +
                ".errorbanner {background-color:Red !important; border-radius:5px 5px 5px 5px; color:Black; padding:5px; font-weight:bold; margin-bottom:10px;margin-top:10px; }" +
                ".filtername {margin:10px 0px 5px 0px; font-size:0.9em; color:#cc33cc;font-weight:bold;}" +
                ".filterborder {display: block; width: 100% - 40px; border-color:#cc33cc; background-color:[FiltContBack] !important; border-width:1px; border-style:solid; padding:5px; margin:0px 0px 5px 0px; border-radius:5px; }" +
                ".filterset {float: left; font-size:0.85em; font-weight:bold; color:#cc33cc; background-color:[FiltContBack] !important; border-width:0px; border-style:none; padding: 0px 3px; margin: 2px 0px 0px 5px; border-radius:3px; }" +
                ".filteractivityborder {background-color:[FiltContActivityBack] !important; color:#fff; }" +
                ".filter {float: left; border-color:#cc33cc; background-color:#cc33cc !important; color:white; border-width:1px; border-style:solid; padding: 0px 5px 0px 5px; font-weight:bold; margin: 0px 5px 0px 5px;  border-radius:3px;}" +
                ".filtererror {float: left; border-color:red; background-color:red !important; color:white; border-width:1px; border-style:solid; padding: 0px 5px 0px 5px; font-weight:bold; margin: 0px 5px 0px 5px;  border-radius:3px;}" +
                ".filebanner {background-color:green !important; border-radius:5px 5px 0px 0px; color:mintcream; padding:5px; font-weight:bold }" +
                ".filecontent {background-color:[ContFileBack] !important; margin-bottom:20px; border-radius:0px 0px 5px 5px; border-color:green; border-width:1px; border-style:none solid solid solid; padding:10px;}" +
                ".defaultbanner {background-color:[ContDefaultBanner] !important; border-radius:5px 5px 0px 0px; color:white; padding:5px; font-weight:bold }" +
                ".defaultcontent {background-color:[ContDefaultBack] !important; margin-bottom:20px; border-radius:0px 0px 5px 5px; border-color:[ContDefaultBanner]; border-width:1px; border-style:none solid solid solid; padding:10px;}" +
                ".holdermain {margin: 20px 0px 20px 0px}" +
                ".holdersub {margin: 5px 0px 5px}" +
                "@media print { body { -webkit - print - color - adjust: exact; }}"+
                "\n</style>\n<!-- graphscript --></ head>\n<body>";

            // apply theme based settings
            if(!Utility.Configuration.Settings.DarkTheme)
            {
                // light theme
                htmlString = htmlString.Replace("[FontColor]", "black");
                htmlString = htmlString.Replace("[HeaderFontColor]", "white");

                // resources
                htmlString = htmlString.Replace("[ResRowBack]", "floralwhite");
                htmlString = htmlString.Replace("[ResRowBack2]", "white");
                htmlString = htmlString.Replace("[ResContBack]", "floralwhite");
                htmlString = htmlString.Replace("[ResContBackLight]", "white");
                htmlString = htmlString.Replace("[ResContBackDark]", "floralwhite");
                htmlString = htmlString.Replace("[ResFontBanner]", "white");
                htmlString = htmlString.Replace("[ResFontContent]", "black");

                //activities
                htmlString = htmlString.Replace("[ActContBack]", "#fdffff");
                htmlString = htmlString.Replace("[ActContBackLight]", "#ffffff");
                htmlString = htmlString.Replace("[ActContBackDark]", "#fdffff");
                htmlString = htmlString.Replace("[ActContBackGroups]", "#ffffff");

                htmlString = htmlString.Replace("[ContDefaultBack]", "#FAFAFA");
                htmlString = htmlString.Replace("[ContDefaultBanner]", "#000");

                htmlString = htmlString.Replace("[ContFileBack]", "#FCFFFC");

                htmlString = htmlString.Replace("[CropRotationBack]", "#FFFFFF");
                htmlString = htmlString.Replace("[LabourGroupBack]", "#FFFFFF");
                htmlString = htmlString.Replace("[LabourGroupBorder]", "#996633");

                // filters
                htmlString = htmlString.Replace("[FiltContBack]", "#fbe8fc");
                htmlString = htmlString.Replace("[FiltContActivityBack]", "#cc33cc");

                // values
                htmlString = htmlString.Replace("[ValueSetBack]", "#e8fbfc");
                htmlString = htmlString.Replace("[ValueSetFont]", "#000000");
            }
            else
            {
                // dark theme
                htmlString = htmlString.Replace("[FontColor]", "#E5E5E5");
                htmlString = htmlString.Replace("[HeaderFontColor]", "black");

                // resources
                htmlString = htmlString.Replace("[ResRowBack]", "#281A0E");
                htmlString = htmlString.Replace("[ResRowBack2]", "#3F2817");
                htmlString = htmlString.Replace("[ResContBack]", "#281A0E");
                htmlString = htmlString.Replace("[ResContBackLight]", "#3F2817");
                htmlString = htmlString.Replace("[ResContBackDark]", "#281A0E");
                htmlString = htmlString.Replace("[ResFontBanner]", "#ffffff"); 
                htmlString = htmlString.Replace("[ResFontContent]", "#ffffff"); 

                //activities
                htmlString = htmlString.Replace("[ActContBack]", "#003F3D");
                htmlString = htmlString.Replace("[ActContBackLight]", "#005954");
                htmlString = htmlString.Replace("[ActContBackDark]", "#f003F3D");
                htmlString = htmlString.Replace("[ActContBackGroups]", "#f003F3D");

                htmlString = htmlString.Replace("[ContDefaultBack]", "#282828");
                htmlString = htmlString.Replace("[ContDefaultBanner]", "#686868");

                htmlString = htmlString.Replace("[ContFileBack]", "#0C440C");

                htmlString = htmlString.Replace("[CropRotationBack]", "#97B2B1");
                htmlString = htmlString.Replace("[LabourGroupBack]", "#c1946c");
                htmlString = htmlString.Replace("[LabourGroupBorder]", "#c1946c");

                // filters
                htmlString = htmlString.Replace("[FiltContBack]", "#5c195e");
                htmlString = htmlString.Replace("[FiltContActivityBack]", "#cc33cc");

                // values
                htmlString = htmlString.Replace("[ValueSetBack]", "#49adc4");
                htmlString = htmlString.Replace("[ValueSetFont]", "#0e2023");
            }

            htmlString += "\n<span style=\"font-size:0.8em; font-weight:bold\">You will need to keep refreshing this page to see changes relating to the last component selected</span><br /><br />";
            htmlString += "\n<div class=\"clearfix defaultbanner\">";

            string fullname = model.Name;
            if(model is CLEMModel)
            {
                fullname = (model as CLEMModel).NameWithParent;
            }
            htmlString += $"<div class=\"namediv\">Component {model.GetType().Name} named {fullname}</div>";
            htmlString += $"<div class=\"typediv\">Details</div>";
            htmlString += "</div>";
            htmlString += "\n<div class=\"defaultcontent\">";
            htmlString += $"\n<div class=\"activityentry\">Summary last created on {DateTime.Now.ToShortDateString()} at {DateTime.Now.ToShortTimeString()}<br />";
            htmlString += "\n</div>";
            htmlString += "\n</div>";


            if (modelToSummarise is ZoneCLEM)
            {
                htmlString += (modelToSummarise as ZoneCLEM).GetFullSummary(modelToSummarise, true, htmlString);
            }
            else if (modelToSummarise is Market)
            {
                htmlString += (modelToSummarise as Market).GetFullSummary(modelToSummarise, true, htmlString);
            }
            else
            {
                htmlString += (modelToSummarise as CLEMModel).GetFullSummary(modelToSummarise, false, htmlString);
            }
            htmlString += "\n</body>\n</html>";

            if(htmlString.Contains("<canvas"))
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                StreamReader textStreamReader = new StreamReader(assembly.GetManifestResourceStream("ApsimNG.Presenters.CLEM.Chart.min.js"));
                htmlString = htmlString.Replace("<!-- graphscript -->", $"<script>{textStreamReader.ReadToEnd()}</script>");
            }

            if (!Utility.Configuration.Settings.DarkTheme)
            {
                htmlString = htmlString.Replace("[GraphGridLineColour]", "#eee");
                htmlString = htmlString.Replace("[GraphGridZeroLineColour]", "#999");
                htmlString = htmlString.Replace("[GraphPointColour]", "#00bcd6");
                htmlString = htmlString.Replace("[GraphLineColour]", "#fda50f");
                htmlString = htmlString.Replace("[GraphLabelColour]", "#888");
            }
            else
            {
                // dark theme
                htmlString = htmlString.Replace("[GraphGridLineColour]", "#555");
                htmlString = htmlString.Replace("[GraphGridZeroLineColour]", "#888");
                htmlString = htmlString.Replace("[GraphPointColour]", "#00bcd6");
                htmlString = htmlString.Replace("[GraphLineColour]", "#ff0");
                htmlString = htmlString.Replace("[GraphLabelColour]", "#888");
            }
            return htmlString;
        }

        /// <summary>
        /// Detach the view
        /// </summary>
        public void Detach()
        {
        }
    }
}

