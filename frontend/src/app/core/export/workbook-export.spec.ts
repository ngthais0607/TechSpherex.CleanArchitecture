import { expect, it } from 'vitest';
import { buildWorkbook } from './workbook-export';
it('exports typed numbers and strings without converting formulas', async () => {
  const bytes = await buildWorkbook(
    'Tồn bãi',
    ['Hãng', 'Số lượng'],
    [['=1+1', 3]],
    'Tất cả hãng',
    new Date('2026-10-20T05:00:00Z'),
  );
  const ExcelJS = await import('exceljs');
  const book = new ExcelJS.Workbook();
  await book.xlsx.load(bytes as never);
  expect(book.worksheets[0].getRow(5).getCell(1).value).toBe('=1+1');
  expect(book.worksheets[0].getRow(5).getCell(2).value).toBe(3);
}, 60000);
