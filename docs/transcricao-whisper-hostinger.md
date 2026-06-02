# Transcricao local com Docker e deploy no Hostinger

## O que mudou

O fluxo da avaliacao agora faz isso no backend:

1. recebe o audio gravado no navegador
2. converte para `mp3` usando `ffmpeg`
3. envia o `mp3` para o bucket `avaliacao-audio` no Supabase Storage REST API
4. envia o mesmo `mp3` para um servidor Whisper local em Docker
5. grava a transcricao em `parecer_1`, `parecer_2` ou `parecer_3`
6. grava a nota em `nota_1`, `nota_2` ou `nota_3`
7. grava o caminho do audio em `audio_parecer_1_path`, `audio_parecer_2_path` ou `audio_parecer_3_path`

O mapeamento do jurado continua:

- jurado `1`: `nota_1`, `parecer_1`, `audio_parecer_1_path`
- jurado `2`: `nota_2`, `parecer_2`, `audio_parecer_2_path`
- jurado `3`: `nota_3`, `parecer_3`, `audio_parecer_3_path`

## Ajuste no banco

Antes de rodar a aplicacao, execute este script no Supabase:

Arquivo: [database/20260530_add_audio_parecer_paths.sql](D:/Projetos/AplicacaoFestivalDancando/webAvaliacaoDancando/WebAvaliacaoDancando/database/20260530_add_audio_parecer_paths.sql)

```sql
alter table public.apresentacoes
    add column if not exists audio_parecer_1_path text,
    add column if not exists audio_parecer_2_path text,
    add column if not exists audio_parecer_3_path text;
```

## Configuracao da aplicacao ASP.NET Core

Configure estes valores por `appsettings`, User Secrets ou variaveis de ambiente:

```json
{
  "AudioProcessing": {
    "FfmpegPath": "ffmpeg"
  },
  "Whisper": {
    "BaseUrl": "http://127.0.0.1:9000/",
    "Model": "whisper-1",
    "Language": "pt",
    "ApiKey": "",
    "TimeoutSeconds": 600
  },
  "SupabaseStorage": {
    "ProjectUrl": "https://kxjnljuanbhlopuhvtog.supabase.co",
    "SecretKey": "coloque_aqui_sua_sb_secret",
    "Bucket": "avaliacao-audio"
  }
}
```

Variaveis de ambiente equivalentes:

```bash
AudioProcessing__FfmpegPath=ffmpeg
Whisper__BaseUrl=http://127.0.0.1:9000/
Whisper__Model=whisper-1
Whisper__Language=pt
Whisper__ApiKey=
Whisper__TimeoutSeconds=600
SupabaseStorage__ProjectUrl=https://kxjnljuanbhlopuhvtog.supabase.co
SupabaseStorage__SecretKey=coloque_aqui_sua_sb_secret
SupabaseStorage__Bucket=avaliacao-audio
```

## Subindo o Whisper localmente

Baseado na documentacao oficial do `hwdsl2/whisper-server`, o container expoe `POST /v1/audio/transcriptions`, aceita `mp3`, `webm`, `wav` e outros formatos suportados pelo `ffmpeg`, e pode ser protegido com `WHISPER_API_KEY`. Fonte: [docker-whisper](https://github.com/hwdsl2/docker-whisper).

Arquivos prontos:

- [infra/whisper/docker-compose.yml](D:/Projetos/AplicacaoFestivalDancando/webAvaliacaoDancando/WebAvaliacaoDancando/infra/whisper/docker-compose.yml)
- [infra/whisper/whisper.env.example](D:/Projetos/AplicacaoFestivalDancando/webAvaliacaoDancando/WebAvaliacaoDancando/infra/whisper/whisper.env.example)

Passos:

1. Entre em [infra/whisper](D:/Projetos/AplicacaoFestivalDancando/webAvaliacaoDancando/WebAvaliacaoDancando/infra/whisper)
2. Copie `whisper.env.example` para `whisper.env`
3. Ajuste o modelo, idioma e `WHISPER_API_KEY` se quiser proteger o endpoint
4. Rode:

```bash
docker compose up -d
docker logs whisper
```

Quando o log indicar que o servidor esta pronto, a aplicacao ASP.NET pode chamar `http://127.0.0.1:9000/v1/audio/transcriptions`.

## Rodando em producao no Hostinger

O Hostinger tem template de VPS com Docker e Docker Compose preinstalados, e o Docker Manager aceita compose manual ou por URL. Fontes oficiais:

- [How to Use the Docker VPS Template at Hostinger](https://www.hostinger.com/support/8306612-how-to-use-the-docker-vps-template-at-hostinger/)
- [Hostinger Docker manager for VPS](https://www.hostinger.com/support/12040789-hostinger-docker-manager-for-vps-simplify-your-container-deployments/)

Tambem deixei um compose pronto para subir a aplicacao e o Whisper juntos:

- [infra/hostinger/docker-compose.production.yml](D:/Projetos/AplicacaoFestivalDancando/webAvaliacaoDancando/WebAvaliacaoDancando/infra/hostinger/docker-compose.production.yml)
- [infra/hostinger/.env.example](D:/Projetos/AplicacaoFestivalDancando/webAvaliacaoDancando/WebAvaliacaoDancando/infra/hostinger/.env.example)

### Opcao A: terminal SSH

1. Crie um VPS usando o template Docker
2. Acesse por SSH
3. Entre em [infra/hostinger](D:/Projetos/AplicacaoFestivalDancando/webAvaliacaoDancando/WebAvaliacaoDancando/infra/hostinger)
4. Copie `.env.example` para `.env`
5. Copie [infra/whisper/whisper.env.example](D:/Projetos/AplicacaoFestivalDancando/webAvaliacaoDancando/WebAvaliacaoDancando/infra/whisper/whisper.env.example) para `whisper.env`
6. Ajuste os segredos e o modelo
7. Rode:

```bash
docker compose -f docker-compose.production.yml up -d --build
```

8. A aplicacao sobe em `http://SEU_HOST:8080`

### Opcao B: Docker Manager

1. Abra o painel do VPS
2. Entre em `Docker Manager`
3. Clique em `Compose`
4. Use o conteudo de [infra/hostinger/docker-compose.production.yml](D:/Projetos/AplicacaoFestivalDancando/webAvaliacaoDancando/WebAvaliacaoDancando/infra/hostinger/docker-compose.production.yml)
5. Cadastre as variaveis do arquivo `.env.example`
6. Envie tambem o `whisper.env`
7. Suba o projeto

### Exposicao publica com HTTPS

Se voce quiser expor o Whisper publicamente no Hostinger, mantenha a porta presa ao loopback (`127.0.0.1:9000:9000`) e publique por um proxy reverso com HTTPS. O proprio Hostinger recomenda usar Traefik para expor multiplos projetos Compose e rotear por dominio. Fonte: [Connecting multiple Docker Compose projects using Traefik in Hostinger Docker Manager](https://www.hostinger.com/support/connecting-multiple-docker-compose-projects-using-traefik-in-hostinger-docker-manager/).

Se o Whisper ficar acessivel pela internet:

- configure `WHISPER_API_KEY`
- configure o mesmo valor em `Whisper__ApiKey` na aplicacao
- mantenha o endpoint sem acesso direto em HTTP aberto

## Teste rapido do endpoint Whisper

Depois do container no ar, voce pode testar assim:

```bash
curl http://127.0.0.1:9000/v1/audio/transcriptions \
  -F file=@audio.mp3 \
  -F model=whisper-1 \
  -F language=pt
```

Resposta esperada:

```json
{ "text": "Transcricao aqui" }
```

## Observacoes operacionais

- o bucket `avaliacao-audio` pode ser privado; a outra aplicacao pode recuperar o arquivo com signed URL ou com requisicao autenticada no Storage
- o `secret key` do Supabase deve ficar apenas no backend
- se voce ja expos uma `sb_secret`, rotacione essa chave no painel do Supabase antes de ir para producao
