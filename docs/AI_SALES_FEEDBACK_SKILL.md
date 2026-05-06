# Skill: Fluxo de feedback de impressão em vendas

Use este guia ao alterar telas ou regras de venda relacionadas a impressão, clonagem ou relatório.

## Modelo de dados

- `Sale.PrintFeedback` guarda o feedback atual da venda impressa.
- `Sale.PrintFeedback.FileQuality.Stars` vai de 0 a 5 e avalia a qualidade do arquivo.
- `Sale.PrintFeedback.FileQuality.Reason` explica a nota do arquivo.
- `Sale.PrintFeedback.PrintQuality.Stars` vai de 0 a 5 e guarda a avaliação exibida como "Qualidade da impressão".
- `Sale.PrintFeedback.PrintQuality.Reason` explica a nota de qualidade da impressão.
- `Sale.PrintFeedback.GeneralNotes` guarda observações gerais do produto impresso.
- `Sale.PrintFeedbackHistory` guarda feedbacks herdados de vendas anteriores quando uma venda e clonada.

## Regras atuais

- Ao marcar uma venda como impressa no relatório, o front abre o modal de feedback antes de concluir.
- O modal exige justificativa para qualidade do arquivo e qualidade da impressão.
- Ao salvar o modal, a venda recebe `isPrintConcluded = true`, `printStatus = "Concluded"` e `printFeedback`.
- As telas de nova venda e edição exibem o mesmo formulario de feedback, permitindo gravar ou alterar os campos manualmente.
- A visualização da venda exibe feedback atual e histórico herdado.
- Antes de iniciar o contador de uma venda clonada com `printFeedbackHistory`, a tela de impressão atual deve obrigar o usuário a ler o histórico herdado.
- O relatório exibe um badge de feedback quando a venda tem avaliação atual ou histórico.
- Ao clonar, o usuário precisa confirmar leitura do feedback anterior se a venda base possuir feedback ou histórico.
- O clone carrega `printFeedbackHistory` com o histórico anterior e o feedback atual da venda base, mas limpa `printFeedback` para receber uma nova avaliação ao concluir a nova impressão.

## Arquivos principais

- Backend: `backend/Models/Sale.cs`
- Backend: `backend/Services/SaleService.cs`
- Front utilitário: `frontend/utils/printFeedback.ts`
- Front clone: `frontend/utils/saleDraft.ts`
- Formulário: `frontend/components/sale/PrintFeedbackForm.tsx`
- Resumo/badge: `frontend/components/sale/PrintFeedbackSummary.tsx`
- Relatório: `frontend/app/sales/page.tsx`
- Nova venda: `frontend/app/sales/new/page.tsx`
- Editar venda: `frontend/app/sales/[id]/page.tsx`
- Visualizar venda: `frontend/app/sales/view/[id]/page.tsx`

## Validação recomendada

1. Criar uma venda de teste com dados mínimos.
2. No relatório, colocar a venda como pendente/fila, marcar como impressa e preencher o modal.
3. Verificar o badge no relatório.
4. Abrir a visualização da venda e conferir o feedback.
5. Editar a venda e alterar as notas/textos.
6. Clonar pelo relatório e confirmar que o modal obriga a leitura do feedback.
7. Concluir a venda clonada com novo feedback e comparar com o histórico herdado.
