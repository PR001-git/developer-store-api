/**
 * Demo reference data. The Sales API stores customers, branches and products as external identities (an id and a name)
 * and owns none of them, so the portal offers these for convenience. Any other name and id works too.
 */
export interface CatalogEntry {
  readonly id: string;
  readonly name: string;
}

export interface ProductEntry extends CatalogEntry {
  readonly unitPrice: number;
}

export const CUSTOMERS: readonly CatalogEntry[] = [
  { id: '00000000-0000-4000-8000-c00000000001', name: 'Maria Silva' },
  { id: '00000000-0000-4000-8000-c00000000002', name: 'João Souza' },
  { id: '00000000-0000-4000-8000-c00000000003', name: 'Ana Oliveira' },
  { id: '00000000-0000-4000-8000-c00000000004', name: 'Bar do Zé' },
  { id: '00000000-0000-4000-8000-c00000000005', name: 'Mercado Bom Preço' },
  { id: '00000000-0000-4000-8000-c00000000006', name: 'Restaurante Sabor da Terra' },
  { id: '00000000-0000-4000-8000-c00000000007', name: 'Empório Paulista' },
  { id: '00000000-0000-4000-8000-c00000000008', name: 'Distribuidora Rio Claro' },
];

export const BRANCHES: readonly CatalogEntry[] = [
  { id: '00000000-0000-4000-8000-b00000000001', name: 'Filial Centro' },
  { id: '00000000-0000-4000-8000-b00000000002', name: 'Filial Paulista' },
  { id: '00000000-0000-4000-8000-b00000000003', name: 'Filial Campinas' },
  { id: '00000000-0000-4000-8000-b00000000004', name: 'Filial Rio de Janeiro' },
  { id: '00000000-0000-4000-8000-b00000000005', name: 'Filial Belo Horizonte' },
];

export const PRODUCTS: readonly ProductEntry[] = [
  { id: '00000000-0000-4000-8000-a00000000001', name: 'Cerveja Pilsen 350ml', unitPrice: 4.5 },
  { id: '00000000-0000-4000-8000-a00000000002', name: 'Cerveja Pilsen 600ml', unitPrice: 7.9 },
  { id: '00000000-0000-4000-8000-a00000000003', name: 'Cerveja Puro Malte 350ml', unitPrice: 5.2 },
  { id: '00000000-0000-4000-8000-a00000000004', name: 'Refrigerante Guaraná 2L', unitPrice: 9.5 },
  { id: '00000000-0000-4000-8000-a00000000005', name: 'Água Mineral 500ml', unitPrice: 2.5 },
  { id: '00000000-0000-4000-8000-a00000000006', name: 'Chope Pilsen Barril 30L', unitPrice: 489.9 },
  { id: '00000000-0000-4000-8000-a00000000007', name: 'Energético 250ml', unitPrice: 8.75 },
  { id: '00000000-0000-4000-8000-a00000000008', name: 'Cerveja sem Álcool 350ml', unitPrice: 4.9 },
];
