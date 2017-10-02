using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cod2GSI
{
    public class ScoreUpdateHandler
    {

        private System.Timers.Timer _Timer;


        private StatusObject _LastStatus;

        private object _StatusLock = new object();

        public ScoreUpdateHandler()
        {
            var test = @"map: mp_vacant
num score ping guid                             name            lastmsg address                                              qport rate
--- ----- ---- -------------------------------- --------------- ------- ---------------------------------------------------- ----- -----
  3     0   52                                  [WD]Funty             0 93.142.85.223:51279                                  23884 100000
  4  1050   60                                  [WD]Ghostly           0 5.39.147.253:28960                                   11913 100000
  5     0   51                                  VK | Bur3k            0 141.138.12.14:28960                                   8881 100000";

            var status = ParseResult(test);

            _Timer = new System.Timers.Timer(4000);
            _Timer.Elapsed += async (sender, e) => await HandleTimer();

        }

        public void Start()
        {
            _Timer.Start();
        }


        private async Task HandleTimer()
        {
            try
            {
                var status = SendCommand("status");
                Console.WriteLine(status);

                var resultObject = ParseResult(status);

                lock (_StatusLock)
                {
                    if (!_LastStatus.MapName.Equals(resultObject.MapName, StringComparison.OrdinalIgnoreCase) || _LastStatus == null)
                {
                    //New state
                    foreach (var playerScore in resultObject.PlayerScores)
                    {

                        var cod2event = new Cod2Event()
                        {
                            AddedScore = playerScore.Score,
                            Game = "cod2",
                            Event = "addScore",
                            Adress = playerScore.Address,
                            Guid = playerScore.Guid,
                            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                        };

                        PostScoreUpdate(cod2event);
                    }
                }
                else
                {
                        //TODO 
                        //Old state
                        foreach (var playerScore in resultObject.PlayerScores)
                        {

                            var score = playerScore.Score;


                            var existing = _LastStatus.PlayerScores.Where(a => a.Guid.Equals(playerScore.Guid));

                            if(existing.Any())
                            {
                                var existingScore = existing.First().Score;

                                score = playerScore.Score - existingScore;

                            }

                            if (score > 0)
                            {
                                var cod2event = new Cod2Event()
                                {
                                    AddedScore = playerScore.Score,
                                    Game = "cod2",
                                    Event = "addScore",
                                    Adress = playerScore.Address,
                                    Guid = playerScore.Guid,
                                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                                };


                                PostScoreUpdate(cod2event);
                            }
                        }
                    }
               
                    _LastStatus = resultObject;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }


        private async void PostScoreUpdate(Cod2Event cod2Event)
        {
            var client = new HttpClient();


            var uri = new Uri("http://localhost:3000/api/Cod2Scoreboard");

            await client.PostAsync(uri, new StringContent(JsonConvert.SerializeObject(cod2Event), Encoding.UTF8, "application/json"));

            client.Dispose();
        }


        private static StatusObject ParseResult(string result)
        {
            var statusObject = new StatusObject();

            var splittedStatus = result.Split(new char[] { '\n', '\r' });

            splittedStatus = splittedStatus.Where(a => a.Length > 0).ToArray();

            foreach (var line in splittedStatus)
            {
                try
                {

                    var mapPlaceHolder = "map: ";
                    var headerPlaceHolder = new string[] { "num", "score", "ping", "guid", "name", "lastmsg", "address", "qport", "rate" };
                    var endPlaceHolder = new string[] { "---", "-----", "----", "------", "---------------", "-------", "---------------------", "-----", "-----" };
                    if (line.StartsWith(mapPlaceHolder))
                    {
                        //Map
                        statusObject.MapName = line.Replace(mapPlaceHolder, string.Empty);
                    }
                    else if (!ContainsAllWords(line, headerPlaceHolder) && !ContainsAllWords(line, endPlaceHolder))
                    {
                        //Score
                        var scoreLine = line.Split(" ");

                        scoreLine = scoreLine.Where(a => a.Length > 0).ToArray();


                        var length = scoreLine.Length - 4;

                        var name = string.Empty;

                        for (int i = 3; i < length; i++)
                        {
                            name += scoreLine[i];

                            if (i < length - 1)
                            {
                                name += " ";
                            }
                        }

                        var playerScore = new PlayerScore()
                        {
                            Name = name,
                            Address = scoreLine[scoreLine.Length - 3],
                            Guid = scoreLine[2],
                            Score = int.Parse(scoreLine[1]),
                        };

                        statusObject.PlayerScores.Add(playerScore);

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            return statusObject;
        }


        private static string SendCommand(string rconCommand)
        {
            var Client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);


            var gameServerIP = "127.0.0.1";
            var gameServerPort = 28960;

            var password = "Pizzaistgeil@@321..";

            Client.Connect(gameServerIP, gameServerPort);

            string command = "rcon " + password + " " + rconCommand;


            Byte[] bufferTemp = Encoding.ASCII.GetBytes(command);
            Byte[] bufferSend = new Byte[bufferTemp.Length + 5];


            // Send the standard 5 characters before the messageessage, prevents disconnect
            bufferSend[0] = Byte.Parse("255");
            bufferSend[1] = Byte.Parse("255");
            bufferSend[2] = Byte.Parse("255");
            bufferSend[3] = Byte.Parse("255");
            bufferSend[4] = Byte.Parse("02");

            int j = 5;

            for (int i = 0; i < bufferTemp.Length; i++)
            {
                bufferSend[j] = bufferTemp[i];
                j++;
            }


            //IPEndPoint remoteIpEndPoint IPEndPoint(IPAddress.Any, 0);
            Client.Send(bufferSend, SocketFlags.None);
            string result = "";
            Thread.Sleep(50);
            while (Client.Available > 0)
            {
                // Use a large recieve buffer to make sure we can take the response
                Byte[] bufferRec = new Byte[Client.Available];
                Client.Receive(bufferRec);
                result = result + Encoding.ASCII.GetString(bufferRec).Replace("????print", "").Replace("\0", ""); // Remove whitespace
            }

            //Client.Disconnect(false);
            //Client.Close();
            Client.Dispose();

            return result;


        }

        public static bool ContainsAllWords(string word, string[] keywords)
        {
            foreach (string keyword in keywords)
            {
                if (!word.Contains(keyword))
                {
                    return false;
                }
            }

            return true;
        }

    }
}
