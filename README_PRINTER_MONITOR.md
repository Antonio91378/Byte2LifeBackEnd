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

`BAMBU_INGEST_TOKEN` autentica o coletor local e o worker local. `BAMBU_COMMAND_PIN` protege a criacao de comandos pelo navegador em producao.

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

## Coletor local

Na maquina que esta na mesma rede da impressora, rode o coletor apontando para o Render:

Telemetria:

```powershell
$env:BAMBU_WATCH='1'
$env:BAMBU_FORWARD_URL='https://byte2lifebackend-3.onrender.com/api/printer-monitor/update'
$env:BAMBU_FORWARD_TOKEN='o-mesmo-token-do-render'
node C:\Users\pichau\Documents\Codex\2026-05-04\consegue-abrir-o-aplicativo-bambu-studio\bambu_mqtt_snapshot.js
```

Camera:

```powershell
$env:BAMBU_CAMERA_WATCH='1'
$env:BAMBU_CAMERA_FPS='1'
$env:BAMBU_CAMERA_FORWARD_URL='https://byte2lifebackend-3.onrender.com/api/printer-monitor/camera/frame'
$env:BAMBU_FORWARD_TOKEN='o-mesmo-token-do-render'
node C:\Users\pichau\Documents\Codex\2026-05-04\consegue-abrir-o-aplicativo-bambu-studio\bambu_camera_frame.js
```

`BAMBU_CAMERA_FPS=1` envia cerca de um frame por segundo. Aumente com cuidado porque cada frame passa pelo Render ate o navegador.

Comandos de escrita:

```powershell
$env:BAMBU_COMMANDS_URL='https://byte2lifebackend-3.onrender.com/api/printer-monitor'
$env:BAMBU_FORWARD_TOKEN='o-mesmo-token-do-render'
node C:\Users\pichau\Documents\Codex\2026-05-04\consegue-abrir-o-aplicativo-bambu-studio\bambu_command_worker.js
```

O worker acima fica rodando na maquina da rede local, consulta a fila de comandos no Render e publica na impressora via MQTT local. Nao rode esse worker se voce quiser apenas visualizar dados.

Para testar uma unica consulta da fila sem deixar o processo rodando:

```powershell
$env:BAMBU_COMMAND_ONCE='1'
$env:BAMBU_COMMANDS_URL='https://byte2lifebackend-3.onrender.com/api/printer-monitor'
$env:BAMBU_FORWARD_TOKEN='o-mesmo-token-do-render'
node C:\Users\pichau\Documents\Codex\2026-05-04\consegue-abrir-o-aplicativo-bambu-studio\bambu_command_worker.js
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

Esses comandos entram em fila no backend. A impressora so recebe o comando quando o `bambu_command_worker.js` esta rodando em uma maquina da mesma rede local dela.
