# Guia de Importação de Vendas - Byte2Life

Este documento descreve o formato correto para importar vendas via arquivo CSV no sistema Byte2Life.

## Formato do Arquivo

O arquivo deve ser um **CSV (Comma Separated Values)**, utilizando **ponto e vírgula (;)** como separador.
A primeira linha deve conter o cabeçalho exato conforme descrito abaixo.

### Colunas Obrigatórias

| Coluna | Descrição | Exemplo |
|--------|-----------|---------|
| Descrição | Nome do produto vendido | Vaso Geométrico |
| LinkFilamento | Link de compra do filamento | http://loja.com/pla-azul |
| PrecoFilamento | Preço pago no rolo de filamento (R$) | 120.00 |
| DescricaoFilamento | Nome/Marca do filamento | PLA Azul 3DLab |
| LinkProduto | Link do modelo 3D (STL) | http://thingiverse.com/123 |
| Qualidade | Qualidade de impressão (Draft, Standard, High) | Standard |
| Massa | Peso da peça impressa em gramas | 150 |
| Custo | Custo calculado da peça (R$) | 18.00 |
| ValorVenda | Valor final da venda (R$) | 50.00 |
| Lucro | Lucro líquido (R$) | 32.00 |
| PorcentagemLucro | Margem de lucro (%) | 177% |
| SexoCliente | Sexo do cliente (M/F) | M |
| Categoria | Categoria do cliente (Novo, Recorrente) | Novo |
| NumeroCliente | Telefone do cliente (apenas números) | 11999998888 |
| Impresso | Status de impressão (S/N) | S |
| Entregue | Status de entrega (S/N) | N |
| Pago | Status de pagamento (S/N) | S |
| Tempo | Tempo de impressão | 4h 30m |

### Regras de Importação

1.  **Criação Automática de Filamentos:**
    *   O sistema verifica se o filamento já existe buscando por `DescricaoFilamento` E `LinkFilamento`.
    *   Se não existir, um novo filamento será criado automaticamente com 1000g de massa inicial.
    *   **Importante:** Se o filamento for criado na importação, sua massa restante será deduzida automaticamente pelo peso da venda importada.

2.  **Criação Automática de Clientes:**
    *   O sistema verifica se o cliente já existe buscando pelo `NumeroCliente`.
    *   Se não existir, um novo cliente será criado.

3.  **Validação de Estoque:**
    *   Ao importar uma venda, a massa utilizada (`Massa`) será subtraída do estoque do filamento correspondente.

### Exemplo de CSV

```csv
Descrição;LinkFilamento;PrecoFilamento;DescricaoFilamento;LinkProduto;Qualidade;Massa;Custo;ValorVenda;Lucro;PorcentagemLucro;SexoCliente;Categoria;NumeroCliente;Impresso;Entregue;Pago;Tempo
Vaso Decorativo;http://loja.com/pla;100.00;PLA Branco;http://stl.com/vaso;High;200;20.00;80.00;60.00;300%;F;Novo;11988887777;S;S;S;5h
Suporte Fone;http://loja.com/petg;130.00;PETG Preto;http://stl.com/suporte;Standard;100;13.00;45.00;32.00;246%;M;Recorrente;11977776666;N;N;N;3h
```
