# Monitor remoto da impressora Bambu

## Fluxo

```text
Impressora Bambu na sua rede local
  -> coletor local Node.js
  -> backend Byte2Life no Render
  -> pagina /printer no frontend Vercel
```

A pagina do Vercel nao acessa `localhost` nem `192.168.x.x`. O coletor local envia os dados para o Render, e a pagina le o Render.

## Backend Render

Defina estas variaveis de ambiente no servico do backend:

```text
BAMBU_INGEST_TOKEN=um-token-longo-e-secreto
BAMBU_COMMAND_PIN=um-pin-para-confirmar-comandos-no-site
```

`BAMBU_INGEST_TOKEN` autentica os processos locais que enviam dados e executam a fila. `BAMBU_COMMAND_PIN` protege a criacao de comandos pelo navegador em producao.

Nao coloque o valor real desses segredos neste README se o arquivo for versionado. O valor real deve ficar apenas nas variaveis de ambiente do Render e no script local `start_bambu_online.ps1`.

Endpoints adicionados:

```text
POST /api/printer-monitor/update
GET  /api/printer-monitor/latest
GET  /api/printer-monitor/history
GET  /api/printer-monitor/events
POST /api/printer-monitor/camera/frame
GET  /api/printer-monitor/camera/latest
GET  /api/printer-monitor/camera/status
GET  /api/printer-monitor/commands
POST /api/printer-monitor/commands
POST /api/printer-monitor/commands/next
POST /api/printer-monitor/commands/{id}/complete
```

## Frontend Vercel

A nova pagina esta em:

```text
/printer
```

Ela usa `NEXT_PUBLIC_API_BASE_URL` quando existir. Se nao existir, em producao usa:

```text
https://byte2lifebackend-3.onrender.com
```

Variavel recomendada no Vercel:

```text
NEXT_PUBLIC_API_BASE_URL=https://byte2lifebackend-3.onrender.com
```

## Coletor local

O backend no Render e o frontend no Vercel nao conseguem abrir conexao direta com a impressora, porque ela esta em uma rede privada (`192.168.x.x`). Por isso existe um processo local rodando em uma maquina da mesma rede da impressora.

Esse processo local faz a ponte:

```text
Impressora Bambu
  -> conexao MQTT local TLS 8883 para telemetria e comandos
  -> conexao camera local TLS 6000 para frames JPEG
  -> HTTPS para o backend no Render
```

O access code da Bambu e lido localmente do perfil do Bambu Studio. Esse access code nao precisa ir para Render/Vercel e nao deve ser versionado no projeto.

### Arquivos locais fora do deploy

Estes arquivos ficam na maquina local e nao fazem parte do deploy do Render/Vercel:

```text
C:\Users\pichau\Documents\Codex\2026-05-04\consegue-abrir-o-aplicativo-bambu-studio\start_bambu_online.ps1
C:\Users\pichau\Documents\Codex\2026-05-04\consegue-abrir-o-aplicativo-bambu-studio\bambu_mqtt_agent.js
C:\Users\pichau\Documents\Codex\2026-05-04\consegue-abrir-o-aplicativo-bambu-studio\bambu_camera_frame.js
```

`start_bambu_online.ps1` inicia os dois processos necessarios:

- `bambu_mqtt_agent.js`: leitura MQTT + execucao de comandos da fila
- `bambu_camera_frame.js`: captura frames da camera e envia para o backend

O `bambu_camera_frame.js` roda em modo continuo e reconecta automaticamente quando a conexao local da camera entrega um pacote invalido ou encerra. Isso evita que o site online fique preso no ultimo frame antigo depois de uma falha temporaria do stream.

### Inicializacao automatica no Windows

Foi criado este arquivo na pasta Startup do usuario:

```text
C:\Users\pichau\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup\Byte2Life Bambu Online Collector.cmd
```

Ele chama:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\Users\pichau\Documents\Codex\2026-05-04\consegue-abrir-o-aplicativo-bambu-studio\start_bambu_online.ps1"
```

Isso significa que a comunicacao online sobe automaticamente quando o usuario entra no Windows. Ela nao roda antes do login.

O Agendador de Tarefas do Windows nao foi usado porque o registro via PowerShell retornou acesso negado sem elevacao de administrador.

### Variaveis locais

Telemetria e comandos de escrita:

```powershell
$env:BAMBU_PRINTER_MONITOR_URL='https://byte2lifebackend-3.onrender.com/api/printer-monitor'
$env:BAMBU_FORWARD_TOKEN='o-mesmo-token-do-render'
node C:\Users\pichau\Documents\Codex\2026-05-04\consegue-abrir-o-aplicativo-bambu-studio\bambu_mqtt_agent.js
```

Esse agente usa uma unica conexao MQTT local para leitura e escrita. Isso evita timeout quando o Bambu Studio ja esta aberto e consumindo conexoes com a impressora. Na validacao online, os comandos de ligar e desligar a luz foram executados com sucesso depois dessa mudanca.

Camera:

```powershell
$env:BAMBU_CAMERA_WATCH='1'
$env:BAMBU_CAMERA_FPS='1'
$env:BAMBU_CAMERA_FORWARD_URL='https://byte2lifebackend-3.onrender.com/api/printer-monitor/camera/frame'
$env:BAMBU_FORWARD_TOKEN='o-mesmo-token-do-render'
node C:\Users\pichau\Documents\Codex\2026-05-04\consegue-abrir-o-aplicativo-bambu-studio\bambu_camera_frame.js
```

`BAMBU_CAMERA_FPS=1` envia cerca de um frame por segundo. Aumente com cuidado porque cada frame passa pelo Render ate o navegador.

Para iniciar manualmente tudo de uma vez:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File C:\Users\pichau\Documents\Codex\2026-05-04\consegue-abrir-o-aplicativo-bambu-studio\start_bambu_online.ps1
```

Para testar so uma atualizacao:

```powershell
$env:BAMBU_WATCH='1'
$env:BAMBU_WATCH_COUNT='1'
$env:BAMBU_FORWARD_URL='https://byte2lifebackend-3.onrender.com/api/printer-monitor/update'
$env:BAMBU_FORWARD_TOKEN='o-mesmo-token-do-render'
node C:\Users\pichau\Documents\Codex\2026-05-04\consegue-abrir-o-aplicativo-bambu-studio\bambu_mqtt_snapshot.js
```

Para testar so alguns frames:

```powershell
$env:BAMBU_CAMERA_WATCH='1'
$env:BAMBU_CAMERA_WATCH_COUNT='3'
$env:BAMBU_CAMERA_FORWARD_URL='https://byte2lifebackend-3.onrender.com/api/printer-monitor/camera/frame'
$env:BAMBU_FORWARD_TOKEN='o-mesmo-token-do-render'
node C:\Users\pichau\Documents\Codex\2026-05-04\consegue-abrir-o-aplicativo-bambu-studio\bambu_camera_frame.js
```

## Checklist para deploy

Antes de subir:

- confirmar que o backend no Render tem `BAMBU_INGEST_TOKEN`
- confirmar que o backend no Render tem `BAMBU_COMMAND_PIN`
- confirmar que o frontend no Vercel tem `NEXT_PUBLIC_API_BASE_URL`
- confirmar que `start_bambu_online.ps1` no PC local usa o mesmo token do Render
- confirmar que o arquivo `.cmd` existe na pasta Startup do usuario
- entrar no Windows com o usuario `pichau` ou iniciar manualmente `start_bambu_online.ps1`
- validar `GET /api/printer-monitor/latest` no Render
- validar `GET /api/printer-monitor/camera/status` no Render
- criar um comando simples de luz e confirmar que ele muda para `succeeded`

Se a camera funciona localmente mas nao online, o mais comum e o `bambu_camera_frame.js` nao estar rodando, ter encerrado por erro de stream, ou o `BAMBU_FORWARD_TOKEN` local nao bater com `BAMBU_INGEST_TOKEN` no Render.

Para checar se o Render esta recebendo frames novos:

```powershell
Invoke-WebRequest -UseBasicParsing https://byte2lifebackend-3.onrender.com/api/printer-monitor/camera/status | Select-Object -ExpandProperty Content
```

O campo `receivedAt` precisa avancar a cada poucos segundos enquanto o coletor local esta rodando.

Se comandos ficam em `pending`, o agente local nao esta rodando ou nao esta autenticando no endpoint `/commands/next`.

Se comandos ficam `failed` com timeout MQTT, verifique se existem processos antigos `bambu_mqtt_snapshot.js` e `bambu_command_worker.js` rodando separados. O fluxo correto agora e usar apenas `bambu_mqtt_agent.js` para MQTT.

## Onde criar regras

As regras de alerta ficam em:

```text
backend/Services/PrinterMonitorService.cs
```

Funcao:

```text
DeriveEvents(status)
```

Exemplos de regras:

- erro da impressora quando `PrintError > 0`
- alerta quando `ProgressPercent >= 95`
- alerta de temperatura quando `NozzleC > 260`
- notificacao por webhook, WhatsApp, Telegram ou Discord

## Comandos suportados

A aba `/printer` tem uma guia `Controle` com estes comandos:

- ligar e desligar luz da camara
- pausar, retomar e parar impressao
- mudar temperatura da mesa
- mudar temperatura do bico
- mudar perfil de velocidade
- enviar G-code avancado

Esses comandos entram em fila no backend. A impressora so recebe o comando quando o `bambu_mqtt_agent.js` esta rodando em uma maquina da mesma rede local dela.
