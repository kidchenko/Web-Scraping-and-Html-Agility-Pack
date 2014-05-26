using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

//Namespace para manipular protocolos, como o HTTP por exemplo
using System.Net;

//Import do pacote de comandos para parse de um documento HTML, XML
using HtmlAgilityPack;

//Import do namespace para poder desenvolver um timer
using System.Timers; 

namespace NetCoders.Extrator
{
    /// <summary>
    /// Interaction logic for Extracao.xaml
    /// </summary>
    public partial class Extracao : Window
    {
        public Extracao()
        {
            InitializeComponent();
        }

        private void btnExtrair_Click(object sender, RoutedEventArgs e)
        {
            //Quando o user clicar nesse button, 
            //fazemos o WEB SCRAPING (EXTRAÇÃO DE DADOS)
            //Técnica muito útil quando não temos acesso ao db que está alimentando o site

            //Criar o timer para rodar a cada 10 segundos
            var timer = new Timer(10000);
            timer.Elapsed += (o, args) =>
            {
                timer.Stop();
                //PRIMEIRA ETAPA - PEDIDO HTTP
                //Nessa etapa navegamos até a página que queremos extrair os dados do HTML
                var webClient = new WebClient();

                //Corrigir problema de acentuação, usar UTF8
                webClient.Encoding = Encoding.UTF8;
                var html = webClient.DownloadString("http://leiloesjudiciais.com.br/externo/por-categoria/16");

                //SEGUNDA ETAPA
                //Vamos fazer o mapeamento,
                //A interprecao do HTML retornado da pagina
                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);

                //Após carregar a string HTML dentro da classe de manipulacao de html,
                //comecamos a extracao de dados

                //Para buscar algum elemento atraves do conteudo de alhum atributo dele, 
                //colocar [@nome_atributo='valor_atributo'
                //SelectSingleNode para buscar um elemento
                var cli_pagina =
                    htmlDocument.DocumentNode
                                .SelectSingleNode("html/body/div[@id='cli_pagina']");

                var pesquisa = cli_pagina.SelectSingleNode("div[@id='cli_lotes_area']/div[@id='pesquisa']/div[@class='leilao']");

                //Agora vamos buscar as informacoes de carro, existem varios carros, e varias dives, para buscar
                //varias div usamos SelecNodes
                var carros = pesquisa.SelectNodes("div[@class='linha']");

                //Abre a conxao com o banco atraves do EF
                var context = new EXTRATOREntities();

                //TERCEIRA ETAPA
                //Nessa etapa vamos trirar os textos e jogar
                //Pra memoria para depois persistir no banco
                foreach (var carro in carros)
                {
                    //Cria um novo registro
                    var novoRegistro = new TB_VEICULOS();

                    var info = carro.SelectSingleNode("div[@class='info']");
                    var strongs = info.SelectNodes("strong");
                    var lote = strongs[0].InnerText;
                    var status = strongs[1].InnerText;
                    var local = strongs[2].InnerText;
                    var descricao = info.SelectSingleNode("p/a").InnerText;
                    var link = info.SelectSingleNode("p/a").Attributes["href"].Value;

                    //Navegar para a pagina de detalhes
                    var htmlDetalhes = webClient.DownloadString("http://leiloesjudiciais.com.br" + link);

                    //Pegar os valores do lado direito
                    var valores = carro.SelectNodes("ul/li");
                    var lanceInicial = valores[0].SelectSingleNode("strong").InnerText;
                    var incrementoMinimo = valores[1].SelectSingleNode("strong").InnerText;
                    var numeroLances = valores[2].SelectSingleNode("strong").InnerText;
                    var numeroVisitas = valores[3].SelectSingleNode("strong").InnerText;
                    var avaliacao = valores[4].SelectSingleNode("strong").InnerText;

                    //Preenche a entidade com os dados
                    novoRegistro.NM_LOTE = lote;
                    novoRegistro.DS_STATUS_VEICULO = status;
                    novoRegistro.NM_LOCAL = local;
                    novoRegistro.DS_VEICULO = descricao;
                    novoRegistro.VL_LANCE_INICIAL = lanceInicial;
                    novoRegistro.NR_INSCRICAO_MINIMO = incrementoMinimo;
                    novoRegistro.NR_LANCES = string.IsNullOrEmpty(numeroLances) ? 0 : Convert.ToInt32(numeroLances);
                    novoRegistro.NR_VISITAS = string.IsNullOrEmpty(numeroVisitas) ? 0 : Convert.ToInt32(numeroVisitas);
                    novoRegistro.VL_AVALIACAO = avaliacao;

                    context.TB_VEICULOS.Add(novoRegistro);
                    context.SaveChanges();
                    timer.Start();
                }
            };
            timer.Start();
        }
    }
}
