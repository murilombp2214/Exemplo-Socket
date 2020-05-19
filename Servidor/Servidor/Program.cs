using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Servidor
{
    static class TypesMinion
    {
        public static string PossoProcessar = "PossoProcessar";
        public static string Encontrei = "Encontrei";
        public static string Encerre = "Encerre";
    }


    class Program
    {
        //atributos 
        private static string enderecoIp = GetLocalIPAddress();
        private static int porta = 1024 + 1;

        //wordList
        private static List<string> wordList = new List<string>();

        //index que esta a wordlist
        private static int indexAtualWordList = 0;


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
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }


        class Teste
        {
            public int Idade { get; set; }
        }
        private static void InitServer(string palavraAhSerBuscada)
        {
            try
            {

                //CRIA SERVIDOR
                var endpoint = new IPEndPoint(IPAddress.Parse(enderecoIp), porta);
                var servidor = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                servidor.Bind(endpoint);

                bool ehParaPararOhLaco = false;
                Socket clienteSock = null;
                while (!ehParaPararOhLaco)
                {
                    servidor.Listen(100);
                    clienteSock = servidor.Accept(); //aguarda uma chamada de um minion

                    clienteSock.ReceiveBufferSize = 16384; //tamanho do buffer

                    byte[] dadosCliente = new byte[1024]; //dados recebidos do cliente
                    int tamanhoBytesRecebidos = clienteSock.Receive(dadosCliente, dadosCliente.Length, 0); //tamanho dos bytes recebidos

                    string response = Encoding.ASCII.GetString(dadosCliente, 0, tamanhoBytesRecebidos); //mensagem do cliente

                    string[] elementos = response.Split(';'); // 0 => TypeMinion, 1 => qtdNucleos

                    if (elementos.Length != 2)
                    {
                        clienteSock.Send(Encoding.ASCII.GetBytes("A quantidade de parametros para essa requisição deve ser igual a 2, e o memso é concatenado em uma string dividido por ponto e virgula"));

                    }
                    else
                    {
                        if (elementos[0] == TypesMinion.PossoProcessar)
                        {
                            var listaParaSerProcessada = GetElementosWordList(int.Parse(elementos[1]) * 300);

                            if (listaParaSerProcessada.Count == 0)
                            {
                                ehParaPararOhLaco = true;
                                byte[] bytesEnviarClinte = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(listaParaSerProcessada) + ";" + palavraAhSerBuscada + ";" + TypesMinion.Encerre);
                                clienteSock.Send(bytesEnviarClinte);
                            }
                            else
                            {
                                byte[] bytesEnviarClinte = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(listaParaSerProcessada) + ";" + palavraAhSerBuscada);
                                clienteSock.Send(bytesEnviarClinte);
                            }
                        }
                        else //encontrou a senha na wordlist
                        {
                            string senhaDescriptografada = response.Split(';')[1];
                            Console.WriteLine($"Senha encontrada, senha: {senhaDescriptografada} ");
                            ehParaPararOhLaco = true;
                        }
                    }


                    clienteSock.Close();
                }



            }
            catch (Exception erro)
            {
                Console.WriteLine($"Erro ao executar o servidor, erro com a mensagem \n {erro.Message}");
            }
        }


        private static List<string> GetElementosWordList(int qtdElementos)
        {
            qtdElementos = indexAtualWordList + qtdElementos;
            var listWord = new List<string>();
            while (indexAtualWordList < wordList.Count && indexAtualWordList < qtdElementos)
            {
                listWord.Add(wordList[indexAtualWordList++]);
            }

            return listWord;


        }



        public static void GerarDadosArquivos()
        {
            string diretorio = "E:/Faculdade/SD/Socket/Arquivos/";
            string arquivo = "WordList.txt";

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

            for (decimal i = 0; i  < 100; ++i)
            {
                wordList.Add(Guid.NewGuid().ToString());
            }
            wordList.Add("MURILO");

        }

        static void Main(string[] args)
        {
            Task.Run(() => GerarDadosArquivos());
            string palavraAhSerBuscada = "MURILO";
            Console.WriteLine("*** Servidor ***");
            Task.Run(() => InitServer(palavraAhSerBuscada));
            Console.ReadKey();
        }
    }
}
