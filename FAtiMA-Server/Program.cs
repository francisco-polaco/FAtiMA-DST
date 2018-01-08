using AssetManagerPackage;
using Newtonsoft.Json;
using RolePlayCharacter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;

namespace FAtiMA_Server
{
    class Program
    {
        //A lock to proccess requests non concurrently
        private static Object l = new Object();

        //TODO Support multiple RPCs
        private static RolePlayCharacterAsset Walter;

        static void Main(string[] args)
        {

#if switch
            AssetManager.Instance.Bridge = new BasicIOBridge();

            Console.Write("Loading Character from file... ");
            Walter = RolePlayCharacterAsset.LoadFromFile("./walter.rpc");
            Walter.LoadAssociatedAssets();
            Console.WriteLine("Complete!");

            WebServer ws = new WebServer(SendResponse, "http://localhost:8080/");
            ws.Run();
            Console.WriteLine("Press a key to quit.");
            Console.ReadKey();
            ws.Stop();

            Walter.SaveToFile("./walter-final.rpc");
#else
            //----------------------------------------------------------------//

            AssetManager.Instance.Bridge = new BasicIOBridge();

            Console.Write("Loading Character from file... ");
            Walter = RolePlayCharacterAsset.LoadFromFile("./walter_mcts.rpc");
            Walter.LoadAssociatedAssets();
            Console.WriteLine("Complete!");

            var ws = new WebServer(SendResponse, "http://localhost:8080/");
            ws.Run();
            Console.WriteLine("Press a key to quit.");
            Console.ReadKey();
            ws.Stop();

            Walter.SaveToFile("./walter-final-mcts.rpc");
#endif       
        }

        public static string SendResponse(HttpListenerRequest request)
        {
            lock (l)
            {
                switch (request.RawUrl)
                {
                    case "/perceptions":
                        if (request.HasEntityBody)
                        {
                            using (System.IO.Stream body = request.InputStream) // here we have data
                            {
                                using (System.IO.StreamReader reader = new System.IO.StreamReader(body, request.ContentEncoding))
                                {
                                    string e = reader.ReadToEnd();
                                    var p = JsonConvert.DeserializeObject<Perceptions>(e);
                                    try
                                    {
                                        p.UpdatePerceptions(Walter);
#if !DEBUG
                                        Console.WriteLine("New percetion");
                                        var array = JObject.Parse(e)["Vision"].ToString();
                                        dynamic dynJson = JsonConvert.DeserializeObject(array);
                                        foreach (var print in dynJson)
                                        {
                                            Console.WriteLine(print);
                                            Console.WriteLine(JObject.Parse(print)["Prefab"] + " " + JObject.Parse(print)["GUID"]);
                                        }
#endif
                                    }
                                    catch (Exception excpt)
                                    {
                                        //Debug.WriteLine(p.ToString());
                                        throw new Exception(p.ToString());
                                    }
                                    return JsonConvert.True;
                                }
                            }
                        }
                        return JsonConvert.False;
                    case "/decide":



                        var decision = Walter.Decide();

                        //var action = decision.FirstOrDefault();
                        //foreach (var a in decision) {
                        //    Console.WriteLine("MODA TUTORIAL: Action: " + a.Name.ToString() + " Target " + a.Target + " Utility: " + a.Utility);
                        //}

                        if (decision.Count() < 1)
                        {
#if DEBUG
                            Console.WriteLine("No decision");
#endif
                            return JsonConvert.Null;
                        }

                        //Console.WriteLine("Before conversion: " + decision.FirstOrDefault().ToString());
                        var action = Action.ToAction(decision.First());
                        //Console.WriteLine("ACTION conversion: " + action.Name + " " + action.Target + "; ");
                        string t = decision.Count().ToString() + ": ";
                        foreach (var a in decision)
                        {
                            t += a.Name + " = " + a.Target + "; ";
                        }
                        Console.WriteLine(t);
                        return JsonConvert.SerializeObject(action);
                    case "/events":
                        if (request.HasEntityBody)
                        {
                            using (System.IO.Stream body = request.InputStream) // here we have data
                            {
                                using (System.IO.StreamReader reader = new System.IO.StreamReader(body, request.ContentEncoding))
                                {
                                    string s = reader.ReadToEnd();
                                    var e = JsonConvert.DeserializeObject<Event>(s);
                                    try
                                    {
                                        e.Perceive(Walter);
                                    }
                                    catch (Exception excpt)
                                    {
                                        //Debug.WriteLine(e.ToString());
                                        throw new Exception(e.ToString());
                                    }
                                    return JsonConvert.True;
                                }
                            }
                        }
                        return JsonConvert.False;
                    default:
                        return JsonConvert.Null;
                }
            }
        }
    }
}
