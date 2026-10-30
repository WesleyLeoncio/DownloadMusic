# DownloadYoutube

Aplicativo desktop para Windows (WPF) que baixa vídeos e playlists do YouTube em dois formatos:

- **Áudio (MP3):** extrai o melhor áudio disponível e converte para MP3 (192 kbps, 44,1 kHz).
- **Vídeo completo (MKV):** baixa o vídeo na maior qualidade e **uma trilha de áudio por idioma** (por exemplo português, inglês e espanhol), sem repetir idiomas, e junta tudo em um único `.mkv` sem recodificar.

## Funcionalidades

- Busca por link de vídeo ou de playlist.
- Lista os itens encontrados, com título, duração e status de cada um.
- Remoção de itens da lista antes de baixar.
- Download de vários itens em paralelo (5 para MP3, 2 para vídeo).
- Barra de progresso geral e mensagens de erro por item.
- Registro de erros em `erros.log`, na pasta do executável.

## Requisitos

| Requisito | Detalhe |
|---|---|
| Sistema operacional | Windows (o projeto usa WPF) |
| .NET | .NET 9 SDK (`net9.0-windows`) para compilar; Runtime Desktop para executar |
| FFmpeg | 7.1.1 *essentials build*, **não incluído no repositório** (veja abaixo) |
| IDE (opcional) | Visual Studio 2022 ou JetBrains Rider |

## Dependências

### Pacotes NuGet

| Pacote | Para que serve |
|---|---|
| [YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode) | Acesso aos dados, streams e trilhas de áudio do YouTube |
| [Ookii.Dialogs.Wpf](https://github.com/ookii-dialogs/ookii-dialogs-wpf) | Janela de seleção de pasta |
| [WpfAnimatedGif](https://github.com/XamlAnimatedGif/WpfAnimatedGif) | GIF animado da tela de carregamento |

Os pacotes são restaurados automaticamente no build.

### FFmpeg (instalação manual)

O FFmpeg é chamado como processo externo para converter para MP3 e para juntar vídeo e áudios no MKV. Por ser um binário grande e ter licença GPL, ele **não é versionado** neste repositório.

1. Baixe o FFmpeg 7.1.1, *essentials build*, para Windows (por exemplo em [gyan.dev/ffmpeg/builds](https://www.gyan.dev/ffmpeg/builds/)).
2. Extraia dentro do projeto, mantendo exatamente este caminho:

```
DownloadYoutube/Libs/FFmpeg/ffmpeg-7.1.1-essentials_build/bin/ffmpeg.exe
```

3. O `.csproj` copia a pasta `Libs` para a saída do build. Se você usar outra versão do FFmpeg, o nome da pasta muda e o caminho em `Services/FfmpegService.cs` precisa mudar junto.

## Como compilar e executar

```bash
git clone https://github.com/WesleyLeoncio/DownloadYoutube.git
cd DownloadYoutube

# coloque o FFmpeg em DownloadYoutube/Libs/FFmpeg/... (veja acima)

dotnet build -c Release
dotnet run --project DownloadYoutube
```

Ou abra `DownloadYoutube.sln` no Visual Studio ou no Rider e execute.

## Como usar

1. Clique em **Pasta** e escolha onde salvar os arquivos.
2. Cole o link do vídeo ou da playlist e clique em **Buscar**.
3. Escolha o formato: **Áudio (MP3)** ou **Vídeo completo (MKV)**.
4. Remova da lista o que não quiser baixar.
5. Clique em **Baixar** e acompanhe o progresso.

O formato e a pasta são lidos uma única vez, no início do download, e a escolha do formato fica bloqueada até terminar.

## Estrutura do projeto

```
DownloadYoutube/
├── DownloadYoutube.sln
└── DownloadYoutube/
    ├── IMG/                 # GIF de carregamento
    ├── Libs/FFmpeg/         # FFmpeg (instalação manual, fora do Git)
    ├── Models/              # VideoItem (item exibido na lista)
    ├── Others/Enums/        # Emoji (status) e FormatoSaida
    ├── Services/
    │   ├── IDownloader.cs        # contrato dos downloaders
    │   ├── AudioDownloader.cs    # download + conversão para MP3
    │   ├── VideoDownloader.cs    # vídeo + trilhas por idioma em MKV
    │   ├── DownloaderFactory.cs  # escolhe o downloader pelo formato
    │   ├── FfmpegService.cs      # chamadas ao ffmpeg.exe
    │   ├── SeletorDeTrilhas.cs   # regra: um áudio por idioma
    │   └── ArquivoUtil.cs        # nome seguro e pastas temporárias
    └── Windows/             # MainWindow (interface) e caixas de mensagem
```

A janela principal cuida apenas da interface e delega o download a um `IDownloader`. Para adicionar um formato novo (por exemplo MP4), basta criar uma classe que implemente `IDownloader`, incluir um valor em `FormatoSaida` e uma linha em `DownloaderFactory`.

## Limitações conhecidas

- **Trilhas de vídeo com vários idiomas:** a maioria dos vídeos tem uma única trilha. Várias trilhas só aparecem em vídeos com dublagem do próprio canal ou dublagem automática do YouTube.
- **Dependência do YouTube:** o YoutubeExplode depende de como o YouTube funciona hoje. Se aparecerem erros como "Video is not available" em vídeos que abrem no navegador, atualize o pacote:

```bash
dotnet add package YoutubeExplode
```

- **Vídeos privados, removidos, com restrição de idade ou bloqueados na região** não podem ser baixados.
- **Playlists grandes** demoram para listar, porque os detalhes de cada vídeo são consultados individualmente.
- **MKV:** é o contêiner usado porque aceita várias trilhas de áudio e qualquer codec que o YouTube entregue. Em players como o VLC, a troca de idioma é feita pelo menu de áudio.

## Aviso legal

Este projeto é de uso pessoal e educacional. Baixar conteúdo do YouTube pode violar os Termos de Serviço da plataforma e os direitos autorais dos criadores. Use apenas com conteúdo que você tem direito de baixar. O autor não se responsabiliza pelo uso indevido da ferramenta.

## Licença

Defina aqui a licença do projeto (por exemplo MIT). O FFmpeg segue a licença própria dele.
