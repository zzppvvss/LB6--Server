using System;
using System.IO;
using System.Text.Json;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class CLAB6_Server
{
    static string basePath = "D:\\saves"; // Базовий шлях для збереження аудіофайлів
    static HttpListener httpListener = new HttpListener(); // Об'єкт для прослуховування HTTP-запитів
    static int HttpPort = 8080; 
    static int UdpPort = 12345; 

    static void Main()
    {
        httpListener.Prefixes.Add($"http://localhost:{HttpPort}/"); // Додаємо префікс для HTTP-прослуховування

        Thread udpThread = new Thread(runUDPServer); // Створюємо потік для запуску UDP-сервера
        udpThread.Start();

        runHTTPServer(); 
    }

    static void runHTTPServer()
    {
        try
        {
            httpListener.Start(); // Запускаємо HTTP-сервер
            Console.WriteLine("HTTP server started. Listening for incoming requests...");

            while (true)
            {
                HttpListenerContext context = httpListener.GetContext(); // Отримуємо контекст вхідного HTTP-запиту

                try
                {
                    if (context.Request.HttpMethod == "POST" && context.Request.Url.AbsolutePath == "/add-audio")
                    {
                        try
                        {
                            string uid = Guid.NewGuid().ToString();
                            string filePath = $"{basePath}/audio_{uid}.mp3"; // Шлях для збереження аудіофайлу з унікальним ідентифікатором

                            using (FileStream fileStream = File.Create(filePath))
                            {
                                context.Request.InputStream.CopyTo(fileStream); // Зберігаємо отриманий аудіофайл
                                Console.WriteLine("Audio file received and saved successfully.");
                            }

                            context.Response.StatusCode = 200; // Встановлюємо код успішного виконання відповіді сервера
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("An error occurred while processing the request: " + ex.Message);
                            context.Response.StatusCode = 500; // Встановлюємо код помилки відповіді сервера
                        }
                    }
                    else if (context.Request.HttpMethod == "GET" && context.Request.Url.AbsolutePath == "/get-audios")
                    {
                        try
                        {
                            string[] files = Directory.GetFiles(basePath); // Отримуємо список аудіофайлів з базового шляху

                            foreach (string file in files)
                            {
                                Console.WriteLine(file);
                            }

                            string jsonResponse = JsonSerializer.Serialize(files); // Перетворюємо список у формат JSON
                            byte[] responseBytes = Encoding.UTF8.GetBytes(jsonResponse); // Кодуємо JSON-відповідь у байтовий масив

                            context.Response.StatusCode = 200; // Встановлюємо код успішного виконання відповіді сервера
                            context.Response.ContentType = "application/json"; // Встановлюємо тип контенту відповіді
                            context.Response.ContentEncoding = Encoding.UTF8; // Встановлюємо кодування відповіді
                            context.Response.ContentLength64 = responseBytes.Length; // Встановлюємо довжину контенту відповіді

                            context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length); // Надсилаємо JSON-відповідь клієнту
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("An error occurred while processing the request: " + ex.Message);
                            context.Response.StatusCode = 500;
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 404; // Встановлюємо код помилки "Сторінку не знайдено"
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred while processing the request: " + ex.Message);
                    context.Response.StatusCode = 500; // Встановлюємо код помилки відповіді сервера
                }
                finally
                {
                    context.Response.Close(); // Закриваємо відповідь сервера
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);
        }
        finally
        {
            httpListener.Stop(); // Зупиняємо HTTP-сервер
        }
    }

    static void runUDPServer()
    {
        Console.WriteLine("UDP server started. Listening for incoming messages...");

        using (UdpClient udpClient = new UdpClient(UdpPort)) // Створюємо UDP-клієнт для прийому та відправлення даних
        {
            while (true)
            {
                IPEndPoint clientEndpoint = new IPEndPoint(IPAddress.Any, UdpPort);

                try
                {
                    byte[] filePathBytes = udpClient.Receive(ref clientEndpoint); // Отримуємо шлях аудіофайлу, відправленого клієнтом
                    string filePath = Encoding.UTF8.GetString(filePathBytes); // Декодуємо шлях аудіофайлу

                    byte[] audioData = File.ReadAllBytes(filePath); // Зчитуємо аудіодані з файлу

                    udpClient.Send(audioData, audioData.Length, clientEndpoint); // Надсилаємо аудіодані клієнту через UDP

                    Console.WriteLine("Audio file sent successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error occurred: " + ex.Message);
                }
            }
        }
    }
}
