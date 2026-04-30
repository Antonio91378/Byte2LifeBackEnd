# Ambiente Local de Validacao

Este documento define o ambiente padrao para trabalhos locais de desenvolvimento, depuracao, validacao visual e prompts futuros neste repositorio.

## Regra Padrao

Para qualquer trabalho local neste projeto, o banco padrao deve ser:

- `Byte2Life_VisualValidation_20260427`

Nao use o banco principal `Byte2Life` para tarefas locais rotineiras, validacao visual, criacao de dados ficticios ou depuracao, a menos que o usuario peca isso explicitamente.

## O Que Este Ambiente E

Este ambiente nao usa mock em memoria.

Ele funciona assim:

- frontend local
- backend local real
- MongoDB real
- database isolado apenas para validacao local

Ou seja: a aplicacao continua usando os services, controllers e regras reais. O isolamento acontece apenas trocando o nome do database usado pela API local.

## Origem da Conexao

A connection string do Mongo vem da configuracao local do backend.

Arquivo relevante:

- `backend/appsettings.Development.Local.json`

Esse arquivo fornece a connection string do Mongo para o ambiente de desenvolvimento. O database ativo e sobrescrito por variavel de ambiente quando queremos usar a base isolada de validacao.

## Database Padrao Para Desenvolvimento Local

Use sempre:

- `MongoDBSettings__DatabaseName=Byte2Life_VisualValidation_20260427`

Isso permite:

- criar dados ficticios sem tocar a base principal
- validar UI sem poluir dados reais
- testar migracoes e ajustes de regra de negocio localmente
- repetir cenarios de debugging com seguranca

## Como Subir o Backend Local

PowerShell:

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:MongoDBSettings__DatabaseName='Byte2Life_VisualValidation_20260427'
Set-Location 'c:/Users/pichau/Desktop/Byte2Life'
dotnet run --launch-profile http --project backend/Byte2Life.API.csproj
```

Opcional com hot reload:

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:MongoDBSettings__DatabaseName='Byte2Life_VisualValidation_20260427'
Set-Location 'c:/Users/pichau/Desktop/Byte2Life/backend'
dotnet watch run
```

Backend esperado:

- `http://localhost:5000`

## Como Validar Que o Backend Correto Esta Ativo

Use o healthcheck:

```powershell
Invoke-RestMethod -Uri 'http://localhost:5000/health/mongo'
```

O retorno esperado deve incluir:

- `database = Byte2Life_VisualValidation_20260427`

Se o healthcheck mostrar `Byte2Life`, voce esta apontando para a base principal e deve corrigir isso antes de continuar.

## Como Subir o Frontend Local

### Desenvolvimento normal

Em `npm run dev`, o frontend ja tende a usar a API local porque `frontend/utils/api.ts` resolve `http://localhost:5000` quando `NODE_ENV=development`.

Exemplo:

```powershell
Set-Location 'c:/Users/pichau/Desktop/Byte2Life/frontend'
npm run dev -- --hostname 127.0.0.1 --port 3000
```

### Preview de producao local

Para `next build` e `next start`, a origem da API precisa estar presente no build.

Use sempre:

```powershell
Set-Location 'c:/Users/pichau/Desktop/Byte2Life/frontend'
$env:NEXT_PUBLIC_API_BASE_URL='http://localhost:5000'
npm run build
npx next start --hostname 127.0.0.1 --port 3001
```

Sem essa variavel no build, o frontend pode embutir a origem remota de producao em rotas client-side e invalidar a validacao local.

## Regra Para Dados Ficticios

Sempre que for necessario criar dados de simulacao:

- grave os dados no database `Byte2Life_VisualValidation_20260427`
- nunca grave esses dados no database principal sem autorizacao explicita
- prefira identificar registros de simulacao no nome ou descricao

## Regra Para Prompts Futuros

Para futuros trabalhos locais neste repositorio, assuma como padrao:

1. backend local em `localhost:5000`
2. frontend local apontando para `localhost:5000`
3. database padrao `Byte2Life_VisualValidation_20260427`
4. dados ficticios e validacoes visuais sempre na base isolada

So saia desse padrao se o usuario pedir explicitamente para usar outro ambiente, outro database ou a base principal.

## Resumo Operacional

Em tarefas locais de desenvolvimento, debugging, validacao visual ou criacao de cenarios:

- use a API local real
- use o Mongo real
- use o database isolado `Byte2Life_VisualValidation_20260427`
- evite qualquer escrita no database principal `Byte2Life`
