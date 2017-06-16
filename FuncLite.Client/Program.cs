using FuncLite.Client.BackendHelper;
using FuncLite.Client.BackendHelper.Models;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

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

            cli.RegisterRun();

            cli.RegisterPublish();
            cli.RegisterDelete();

            cli.RegisterGetVersions();

           // cli.RegisterGetLogs();


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
                functionCommand = GetFunctionCommand();
            }

            public int Parse(string[] args)
            {
                try
                {
                    if (args.Length == 0)
                    {
                        application.ShowHint();
                        throw new CommandParsingException(application,"No command was passed");
                    }
                    return application.Execute(args);
                }
                catch (CommandParsingException e)
                {
                    Console.WriteLine(e.Command.GetHelpText());
                    return 1;
                }
            }

          

            private void NormalizeCommand(CommandLineApplication command, string description, Func<Task<int>> onExecuteFunction)
            {
                command.HelpOption("--help");
                command.Description = description;
                CommandOption debugOption = command.Option("--debug", "Sets logs to verbose", CommandOptionType.NoValue);
                debugOption.ShowInHelpText = false;
                CommandOption hostOption = command.Option("--hostUrl", "Sets the host for the controller", CommandOptionType.SingleValue);
                hostOption.ShowInHelpText = false;

                command.OnExecute(async () =>
                {
                    if (command.Arguments.Exists(arg => arg.Value == null))
                    {
                        Console.WriteLine(command.GetHelpText());
                        return 1;
                    }
                    if (debugOption.HasValue())
                    {
                        Console.WriteLine("Running command: " + command.Name + "...");
                    }

                    try
                    {

                        if (hostOption.HasValue())
                        {
                            client = new FunctionsLite(new Uri(hostOption.Value()));
                        }
                        return await onExecuteFunction.Invoke();
                    }
                    catch (Exception e) when (e is Microsoft.Rest.HttpOperationException || e is UriFormatException)
                    {
                        Console.Error.WriteLine("Error communicating with server");
                        if (debugOption.HasValue())
                        {
                            Console.Error.WriteLine(e.Message);
                            Console.Error.WriteLine(e.StackTrace);
                        }
                        return 1;
                    }
                    
                });
            }

            public void RegisterList()
            {
                application.Command("list", (command) => {
                   NormalizeCommand(command, "Lists your functions", async () =>
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
                    NormalizeCommand(command, "Publishes function code, creating the function if necessary", async () =>
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
                            language = "node";
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
                    NormalizeCommand(command, "Lists the versions for a function", async () =>
                    {
                        IList<string> versions = await client.ListVersionsForFunctionAsync(name.Value);
                        foreach (string v in versions)
                        {
                            Console.WriteLine(v);
                        }
                        return 0;
                    });
                });
            }

            private CommandLineApplication GetFunctionCommand()
            {
                CommandArgument name = null;
                CommandOption version = null;
                return application.Command("function", (command) => {
                    name = command.Argument("functionName", "The name of the target function");
                    version = command.Option("-v | --version", "A specific version to read", CommandOptionType.SingleValue);
                    NormalizeCommand(command, "Gets details about a function and exposes additional commands", async () =>
                    {
                        if (version.HasValue())
                        {
                            VersionInfo v = await client.GetVersionOfFunctionAsync(name.Value, version.Value());
                            Console.WriteLine(v.ToString());
                        }
                        else
                        {
                            Function function = await client.GetFunctionAsync(name.Value);
                            Console.WriteLine(function.Name);
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
                    NormalizeCommand(command,"Deletes a function", async () =>
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
                functionCommand.Command("*logs", (command) => {
                    name = command.Argument("functionName", "The name of the target function");
                    invocation = command.Option("-i | --invocation", "A specific invocation to get logs for", CommandOptionType.SingleValue);
                    NormalizeCommand(command, "UNIMPLEMENTED - Gets the logs for a function", async () =>
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


            
            public void RegisterRun()
            {
                CommandArgument name = null; 
                CommandOption version = null;
                CommandOption fileName = null;
                functionCommand.Command("run", (command) =>
                {
                    name = command.Argument("functionName", "The name of the target function");
                    version = command.Option("-v | --version", "A specific version to run", CommandOptionType.SingleValue);
                    fileName = command.Option("-f | --file", "The name of a JSON file to be used as the request body", CommandOptionType.SingleValue);
                    NormalizeCommand(command, "Runs a function in the cloud", async () =>
                    {
                        HttpClient customClient = new HttpClient();

                        string baseURL = client.BaseUri.AbsoluteUri + "api/functions/" + name.Value;
                        string versionURL = version.HasValue() ? "/versions/"+ version.Value() : "";
                        string runURL = baseURL + versionURL + "/run";
                        Console.WriteLine(runURL);

                        HttpMethod runMethod = HttpMethod.Post;

                        JObject body = new JObject();
                        if (fileName.HasValue())
                        {
                            body = (JObject)JsonConvert.DeserializeObject(File.ReadAllText(fileName.Value()));
                        }

                        HttpRequestMessage request = new HttpRequestMessage(runMethod, runURL);
                        request.Content = new StringContent(body.ToString(), System.Text.Encoding.UTF8, "application/json");
                        Console.WriteLine(request.ToString());
                        Console.WriteLine(await request.Content.ReadAsStringAsync());
                        Console.WriteLine();

                        HttpResponseMessage response = await customClient.SendAsync(request);
                        Console.WriteLine(response.ToString());
                        Console.WriteLine(await response.Content.ReadAsStringAsync());

                        return 0;
                    });
                });
            }

        }

    }
}