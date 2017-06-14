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
        const string backendURL = "http://funclite.azurewebsites.net";
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
            private IFunctionsLite client;
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

          

            private void NormalizeCommand(CommandLineApplication command, Func<Task<int>> onExecuteFunction)
            {
                command.HelpOption("--help");
                command.OnExecute(async () =>
                {
                    if (command.Arguments.Exists(arg => arg.Value == null))
                    {
                        Console.WriteLine(command.GetHelpText());
                        return 1;
                    }
                    Console.WriteLine("Executing " + command.Name + "...");

                    try
                    {
                        return await onExecuteFunction.Invoke();
                    }
                    catch (Microsoft.Rest.HttpOperationException e)
                    {
                        Console.Error.WriteLine("Error communicating with server");
                        Console.Error.WriteLine(e.Message);
                        Console.Error.WriteLine(e.StackTrace);
                        return 1;
                    }
                    
                });
            }

            public void RegisterList()
            {
                application.Command("list", (command) => {
                   NormalizeCommand(command, async () =>
                    {
                        IList<string> functions = await client.ListFunctionsAsync();
                        foreach(string f in functions)
                        {
                            Console.WriteLine(f);
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
                    name = command.Argument("functionName", "The name of the target function");
                    path = command.Argument("path", "The files to be published");
                    NormalizeCommand(command, async () =>
                    {
                        //Load in path and establish language
                        string language = null;
                        var directory = Directory.EnumerateFiles(path.Value);
                        bool hasNode = false;
                        bool hasRuby = false;
                        foreach (string file in directory)
                        {
                            string fileName = file.Substring(path.Value.Length + 1);
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
                        File.Delete(archiveFile);

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
                    NormalizeCommand(command, async () =>
                    {
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
                    NormalizeCommand(command, async () =>
                    {
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
                    NormalizeCommand(command, async () =>
                    {
                        
                        if (ConfirmDeletion(name.Value))
                        {
                            Console.WriteLine("Deleting...");
                            if (version.HasValue())
                            {
                                await client.DeleteVersionOfFunctionAsync(name.Value, version.Value());
                            }
                            else
                            {
                                await client.DeleteFunctionAsync(name.Value);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Deletion cancelled.");
                        }

                        return 0;
                    });
                });
            }

            private bool ConfirmDeletion(string functionName)
            {
                Console.WriteLine("To confirm deletion, please enter the function name");
                string confirmation = Console.ReadLine();
                return functionName.Equals(confirmation);
            }


            public void RegisterGetLogs()
            {
                CommandArgument name = null;
                CommandOption invocation = null;
                functionCommand.Command("logs", (command) => {
                    name = command.Argument("functionName", "The name of the target function");
                    invocation = command.Option("-i | --invocation", "A specific invocation to get logs for", CommandOptionType.SingleValue);
                    NormalizeCommand(command, async () =>
                    {
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