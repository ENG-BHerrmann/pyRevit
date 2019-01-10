﻿using System;
using System.Collections.Generic;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;


namespace PyRevitLoader {
    [Regeneration(RegenerationOption.Manual)]
    [Transaction(TransactionMode.Manual)]
    public class pyRevitRunnerCommand : IExternalCommand {

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements) {

            // grab application and command data, skip elements since this is a batch runner and user doesn't 
            // see the gui to make selections
            Application = commandData.Application;
            CommandData = commandData;


            // 1
            // Processing Journal Data and getting the script path to be executed in IronPython engine
            IDictionary<string, string> dataMap = commandData.JournalData;
            try {
                ScriptSourceFile = dataMap["ScriptSource"];
                ModuleSearchPaths = dataMap["SearchPaths"].Split(';');
                LogFile = dataMap["LogFile"];

                // 2
                // Executing the script
                var executor = new ScriptExecutor(Application); // uiControlledApplication);
                var resultCode = executor.ExecuteScript(ScriptSourceFile,
                                                        sysPaths: ModuleSearchPaths,
                                                        logFilePath: LogFile);

                // 3
                // Log results

                if (resultCode == 0)
                    return Result.Succeeded;
                else
                    return Result.Cancelled;
            }
            catch (Exception) {
                // do nothing
                return Result.Cancelled;
            }
        }

        public UIApplication Application { get; private set; }
        public ExternalCommandData CommandData { get; private set; }

        public string ScriptSourceFile { get; private set; }
        public string[] ModuleSearchPaths { get; private set; }
        public string LogFile { get; private set; }
        public bool DebugMode { get; private set; }
    }
}
