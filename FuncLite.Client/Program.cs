using FuncLite.Client.BackendHelper;
using FuncLite.Client.BackendHelper.Models;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.IO.Compression;

namespace FuncLite.Client
{
    class Program
    {
        const string backendURL = "https://functions.azure.com";
        const string archiveFileName = "funcLitePackage.zip";

        static void Main(string[] args)
        {
            CommandLine cli = new CommandLine(backendURL);


            cli.RegisterList();

            //cli.RegisterRun();

            cli.RegisterPublish();
            cli.RegisterDelete();
            cli.RegisterDetails();

            cli.RegisterGetVersions();

            cli.RegisterGetLogs();


             cli.Parse(args);
 
        }

        public class CommandLine
        {
            private FunctionsLite client;
            private CommandLineApplication application;
            private CommandLineApplication functionCommand;

            public CommandLine(string url)
            {
                client = new FunctionsLite(new Uri(url));
                application = new CommandLineApplication();
                application.HelpOption("-? | -h | --help");
                functionCommand = application.Command("function", (command) =>
                {
                    command.HelpOption("--help");
                    command.OnExecute(() =>
                    {
                        Console.WriteLine(command.GetHelpText());
                        return 1;
                    });
                });
            }

            public int Parse(string[] args)
            {
                try
                {
                    return application.Execute(args);
                }
                catch (CommandParsingException)
                {
                    Console.WriteLine(application.GetHelpText());
                    return 1;
                }
            }

            private void LogExecution(CommandLineApplication command)
            {
                Console.WriteLine("Executing " + command.Name + "...");
            }

            private bool ValidateArguments(CommandLineApplication command)
            {
                if (command.Arguments.Exists(arg => arg.Value == null))
                {
                    Console.WriteLine(command.GetHelpText());
                    return false;
                }
                Console.WriteLine("Executing " + command.Name + "...");
                return true;
            }

            public void RegisterList()
            {
                application.Command("list", (command) => {
                    command.HelpOption("--help");
                    command.OnExecute(async () =>
                    {
                        if (command.Arguments.Exists(arg => arg.Value == null))
                        {
                            Console.WriteLine(command.GetHelpText());
                            return 1;
                        }
                        LogExecution(command);

                        IList<Function> functions = await client.ListFunctionsAsync();
                        foreach(Function f in functions)
                        {
                            Console.WriteLine(f.ToString());
                        }
                        return 0;
                    });
                });
            }


            public void RegisterPublish()
            {
                CommandArgument name = null;
                CommandArgument path = null;
                functionCommand.Command("publish", (command) => {
                    command.HelpOption("--help");
                    name = command.Argument("functionName", "The name of the target function");
                    path = command.Argument("path", "The files to be published");
                    command.OnExecute(async () =>
                    {
                        if (command.Arguments.Exists(arg => arg.Value == null))
                        {
                            Console.WriteLine(command.GetHelpText());
                            return 1;
                        }
                        LogExecution(command);

                        //Load in path and establish language
                        string language = null;
                        Console.WriteLine(path.Value);
                        var directory = Directory.EnumerateFiles(path.Value);
                        bool hasNode = false;
                        bool hasRuby = false;
                        foreach (string file in directory)
                        {
                            string fileName = file.Substring(path.Value.Length + 1);
                            Console.WriteLine(fileName);
                            if (fileName.Equals("index.js")){
                                hasNode = true;
                            }
                            else if (fileName.Equals("function.rb")){
                                hasRuby = true;
                            }
                        }
                        if (hasNode)
                        {
                            language = "javascript";
                        }
                        else if (hasRuby)
                        {
                            language = "ruby";
                        }
                        else
                        {
                            return 1;
                        }

                        //Create zip
                        string archiveFile = Path.GetTempPath() + archiveFileName;
                        Console.WriteLine(archiveFile);
                        ZipFile.CreateFromDirectory(path.Value, archiveFile);
                        Stream zipfile = new FileStream(archiveFile, FileMode.Open);

                        await client.CreateUpdateFunctionAsync(zipfile, language, name.Value);
                        zipfile.Dispose();
                        File.Delete(archiveFile);

                        return 0;
                    });
                });
            }

            public void RegisterGetVersions()
            {
                CommandArgument name = null;
                functionCommand.Command("versions", (command) => {
                    name = command.Argument("functionName", "The name of the target function");
                    command.HelpOption("--help");
                    command.OnExecute(async () =>
                    {

                        if (command.Arguments.Exists(arg => arg.Value == null))
                        {
                            Console.WriteLine(command.GetHelpText());
                            return 1;
                        }
                        LogExecution(command);
                        IList<VersionInfo> versions = await client.ListVersionsForFunctionAsync(name.Value);
                        foreach (VersionInfo v in versions)
                        {
                            Console.WriteLine(v.ToString());
                        }
                        return 0;
                    });
                });
            }

            public void RegisterDetails()
            {
                CommandArgument name = null;
                CommandOption version = null;
                functionCommand.Command("detail", (command) => {
                    name = command.Argument("functionName", "The name of the target function");
                    version = command.Option("-v | --version", "A specific version to read", CommandOptionType.SingleValue);
                    command.HelpOption("--help");
                    command.OnExecute(async () =>
                    {

                        if (command.Arguments.Exists(arg => arg.Value == null))
                        {
                            Console.WriteLine(command.GetHelpText());
                            return 1;
                        }
                        LogExecution(command);
                        if (version.HasValue()) {
                            VersionInfo v = await client.GetVersionOfFunctionAsync(name.Value, version.Value());
                            Console.WriteLine(v.ToString());
                        }
                        else
                        {
                            //TODO
                            Console.WriteLine("NOT YET SUPPORTED");
                        }
                        
                        return 0;
                    });
                });
            }


            public void RegisterDelete()
            {
                CommandArgument name = null;
                CommandOption version = null;
                functionCommand.Command("delete", (command) => {
                    name = command.Argument("functionName", "The name of the target function");
                    version = command.Option("-v | --version", "A specific version to delete", CommandOptionType.SingleValue);
                    command.HelpOption("--help");
                    command.OnExecute(async () =>
                    {

                        if (command.Arguments.Exists(arg => arg.Value == null))
                        {
                            Console.WriteLine(command.GetHelpText());
                            return 1;
                        }
                        LogExecution(command);
                        if (version.HasValue())
                        {
                            //TODO confirmation
                            await client.DeleteVersionOfFunctionAsync(name.Value, version.Value());
                        }
                        else
                        {
                            //TODO confirmation
                            await client.DeleteFunctionAsync(name.Value);
                        }

                        return 0;
                    });
                });
            }


            public void RegisterGetLogs()
            {
                CommandArgument name = null;
                CommandOption invocation = null;
                functionCommand.Command("logs", (command) => {
                    name = command.Argument("functionName", "The name of the target function");
                    invocation = command.Option("-i | --invocation", "A specific invocation to get logs for", CommandOptionType.SingleValue);
                    command.HelpOption("--help");
                    command.OnExecute(async () =>
                    {
                        if (command.Arguments.Exists(arg => arg.Value == null))
                        {
                            Console.WriteLine(command.GetHelpText());
                            return 1;
                        }
                        LogExecution(command);

                        if (invocation.HasValue())
                        {
                            string logs = await client.GetLogForInvocationAsync(name.Value, invocation.Value());
                            Console.WriteLine(logs);
                        }
                        else
                        {
                            string logs = await client.GetLogStreamAsync(name.Value);
                            Console.WriteLine(logs);
                        }
                        return 0;
                    });
                });
            }


            //TODO
            //public void RegisterRun()
            //{
            //    CommandArgument name = null;
            //    CommandOption version = null;
            //    functionCommand.Command("run", (command) => {
            //        name = command.Argument("functionName", "The name of the target function");
            //        version = command.Option("-v | --version", "A specific version to run", CommandOptionType.SingleValue);
            //        command.HelpOption("--help");
            //        command.OnExecute(async () =>
            //        {

            //            if (command.Arguments.Exists(arg => arg.Value == null))
            //            {
            //                Console.WriteLine(command.GetHelpText());
            //                return 1;
            //            }
            //            LogExecution(command);
            //            //if (version.HasValue())
            //            //{

            //            //}
            //            //else
            //            //{

            //            //}
            //            return 0;
            //        });
            //    });
            //}

        }

    }
}