using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cliente
{

    public class Coroutine
    {

        public Func<string, string> FuncaoHash { get; private set; }

        /// <summary>
        /// Caminho do arquivo
        /// </summary>

        public string PathFile { get; set; }

        /// <summary>
        /// Construtor Defualt
        /// </summary>

        public Coroutine(Func<string, string> funcHash)

        {
            FuncaoHash = funcHash;
        }

        /// <summary>
        /// Cria o objeto ja ocm o nome do arquivo
        /// </summary>
        /// <param name="funcHash">Funcao hash</param>
        /// <param name="pathFile">arquivo</param>

        public Coroutine(Func<string, string> funcHash, string pathFile)

        {

            PathFile = pathFile;

            FuncaoHash = funcHash;

        }


        /// <summary>
        /// Ler o arquivo usando yeld return
        /// </summary>
        /// <returns></returns>

        private IEnumerable<string> LerArquivoCoroutine()

        {

            using var st = new StreamReader(path: PathFile);
            string word = string.Empty;
            while ((word = st.ReadLine()) != null)
            {
                yield return FuncaoHash(word);
            }

        }

        /// <summary>
        /// Ler o arquivo usando yeld return assincrono
        /// </summary>
        /// <returns></returns>
        private IEnumerable<string> LerArquivoCoroutineAsync()
        {
            var st = new StreamReader(path: PathFile);
            string word = string.Empty;
            while (word != null)
            {
                word = st.ReadLine();

                if (!string.IsNullOrEmpty(word))
                {
                    yield return FuncaoHash(word);
                }
            }
        }

        /// <summary>
        /// Processa dos dados do arquivo usando a coroutine
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> BusquePorCoroutine()

        {
            foreach (var item in LerArquivoCoroutine())
            {
                yield return item;
            }
        }

        /// <summary>
        /// Processa dos dados do arquivo usando a coroutine assincrona
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> BusquePorCoroutineAsync()
        {
            foreach (var item in LerArquivoCoroutineAsync())
            {
                yield return item;
            }

        }

    }




    class Program
    {
        static class TypesMinion
        {
            public static string PossoProcessar = "PossoProcessar";
            public static string Encontrei = "Encontrei";
            public static string Encerre = "Encerre";
        }
        static bool encontrouHash = false;
        private static Coroutine objCoroutine;

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }

            return "";
        }


        private static void ProcesseMinion(string msgParaServidor, bool pedirTarefa)
        {
            try
            {
                if (encontrouHash)
                {
                    return;
                }

                string enderecoIp = GetLocalIPAddress();
                int porta = 1024 + 1;
                int tamanhoMax = 99999;


                //Conecta com servidor
                TcpClient client = new TcpClient();
                client.Connect(enderecoIp, porta);

                //Envia mensagem para o servidor
                NetworkStream stream = client.GetStream();
                byte[] data = System.Text.Encoding.ASCII.GetBytes(msgParaServidor);
                stream.Write(data, 0, data.Length);
                Console.WriteLine($"Mensagem Enviada: {msgParaServidor}");


                if (pedirTarefa)
                {
                    //Mensagem Recebida servidor
                    byte[] bytesRecebidosServer = new byte[tamanhoMax];
                    int qtdBytesRecebidosServer = stream.Read(bytesRecebidosServer, 0, bytesRecebidosServer.Length);
                    string responseServidor = System.Text.Encoding.ASCII.GetString(bytesRecebidosServer, 0, qtdBytesRecebidosServer);

                    string[] vetRespServ = responseServidor.Split(';');
                    string palavraAhSerBuscada = vetRespServ[1];
                    string list = vetRespServ[0];
                    List<string> wordList = JsonConvert.DeserializeObject<List<string>>(list);

                    //A Lista do servidor acabou ou achou a wordlist
                    if (vetRespServ.Length > 3 && vetRespServ[2] == TypesMinion.Encerre)
                    {
                        return;
                    }

                    if (wordList != null && wordList.Count > 0)
                    {
                        //Processa a wordList
                        string senha = ProcesseWordList(wordList, palavraAhSerBuscada);

                        if (encontrouHash)  //se alguem encontrou não processa mais
                        {
                            return;
                        }

                        if (senha == string.Empty) //senão encontrar a word pede mais uma tarefa ao servidor
                        {
                            Task.Run(() => ProcesseMinion(TypesMinion.PossoProcessar + ";" + Environment.ProcessorCount.ToString(), true));
                        }
                        else //se encontrar uma tarefa avisa o servidor que encontrou
                        {
                            Task.Run(() =>
                            {
                                ProcesseMinion(TypesMinion.Encontrei + ";" + senha, false);
                                encontrouHash = true;
                            });
                        }
                    }
                }


                //fecha as conexões
                stream.Close();
                client.Close();
                Console.ReadKey();
            }
            catch (Exception erro)
            {
                Console.WriteLine("Houve um erro no procesameto do miniom");
            }

        }

        public static string HashSHA256(string value)

        {

            using SHA256 objSha = SHA256.Create();

            byte[] bytes = objSha.ComputeHash(Encoding.UTF8.GetBytes(value));

            StringBuilder Retorno = new StringBuilder();

            for (int i = 0; i < bytes.Length; i++)

                Retorno.Append(bytes[i].ToString());

            return Retorno.ToString().ToUpper();

        }


        private static void CrieArquivoDeWordList(string diretorio, string arquivo, List<string> wordList)
        {
            if (!Directory.Exists(diretorio))
            {
                Directory.CreateDirectory(diretorio);

            }

            if (!File.Exists(Path.Combine(diretorio, arquivo)))
            {
                File.Create(Path.Combine(diretorio, arquivo)).Close();
            }
            else
            {
                File.Delete(Path.Combine(diretorio, arquivo));
                File.Create(Path.Combine(diretorio, arquivo)).Close();
            }

            var streamCript = new StreamWriter(Path.Combine(diretorio, arquivo));

            foreach (var item in wordList)
            {
                streamCript.WriteLine(item);
            }


            streamCript.Close();


        }

        private static string ProcesseWordList(List<string> wordList, string palavraAhSerBuscada)
        {

            string diretorio = "E:/Faculdade/SD/Socket/ArquivosLixo";
            string arquivo = $"{Guid.NewGuid()}.txt";

            CrieArquivoDeWordList(diretorio, arquivo, wordList);

            ParallelOptions po = new ParallelOptions();
            po.CancellationToken = new CancellationTokenSource().Token;
            po.MaxDegreeOfParallelism = System.Environment.ProcessorCount;
            objCoroutine = new Coroutine((value) => HashSHA256(value), Path.Combine(diretorio, arquivo));

            palavraAhSerBuscada = HashSHA256(palavraAhSerBuscada);


            string senhaEncontrada = string.Empty;

            if(totalmeenteEmParalelo)
            {
                Parallel.ForEach(objCoroutine.BusquePorCoroutineAsync(), po, (result, state) =>
                {

                    try
                    {
                        if (!string.IsNullOrEmpty(result))
                        {
                            string hash = HashSHA256(result);
                            Console.WriteLine($"hash => {result} => busca => {palavraAhSerBuscada}");
                            if (hash == palavraAhSerBuscada)
                            {
                                senhaEncontrada = hash;
                            }
                        }
                    }

                    catch (Exception erro)
                    {

                        Console.WriteLine("Houve um erro ao processar essa wordList");
                    }

                });
            }
            else
            {
                foreach (var item in objCoroutine.BusquePorCoroutineAsync())
                {
                    if (item == palavraAhSerBuscada)
                    {
                        senhaEncontrada = item;
                        break;
                    }
                }

            }


            return senhaEncontrada;

        }
        private static bool totalmeenteEmParalelo = false;
        static void Main(string[] args)
        {
            Console.WriteLine("Digite 's' para executar em paralelo.");
            totalmeenteEmParalelo = Console.ReadLine() == "s";
            string msg = TypesMinion.PossoProcessar + ";" + Environment.ProcessorCount.ToString();
            ProcesseMinion(msg, true);
            Console.ReadKey();
        }
    }
}
