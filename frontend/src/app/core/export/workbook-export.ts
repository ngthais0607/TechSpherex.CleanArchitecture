export async function buildWorkbook(
  title: string,
  headers: string[],
  rows: (string | number | Date | null)[][],
  filters: string,
  now: Date,
): Promise<Uint8Array> {
  const ExcelJS = await import('exceljs');
  const book = new ExcelJS.Workbook();
  book.creator = 'SNP Depot Demo';
  book.created = now;
  const sheet = book.addWorksheet('Báo cáo');
  sheet.addRow([title]);
  sheet.addRow(['Điều kiện: ' + filters]);
  sheet.addRow(['Xuất lúc: ' + now.toLocaleString('vi-VN', { timeZone: 'Asia/Bangkok' })]);
  sheet.addRow(headers);
  rows.forEach((r) => sheet.addRow(r));
  sheet.getRow(1).font = { size: 18, bold: true, color: { argb: 'FF0084A0' } };
  sheet.getRow(4).font = { bold: true, color: { argb: 'FFFFFFFF' } };
  sheet.getRow(4).fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FF153249' } };
  sheet.columns.forEach((c, i) => {
    c.width = Math.min(50, Math.max(18, headers[i]?.length + 4 || 18));
  });
  sheet.views = [{ state: 'frozen', ySplit: 4 }];
  sheet.autoFilter = {
    from: { row: 4, column: 1 },
    to: { row: rows.length + 4, column: headers.length },
  };
  sheet.eachRow((row, idx) => {
    if (idx > 4)
      row.eachCell((cell) => {
        if (cell.value instanceof Date) cell.numFmt = 'dd/mm/yyyy';
        else if (typeof cell.value === 'number')
          cell.numFmt = Number.isInteger(cell.value) ? '#,##0' : '#,##0.0';
      });
  });
  return new Uint8Array(await book.xlsx.writeBuffer());
}
export async function downloadWorkbook(
  title: string,
  headers: string[],
  rows: (string | number | Date | null)[][],
  filters: string,
) {
  const bytes = await buildWorkbook(title, headers, rows, filters, new Date());
  const url = URL.createObjectURL(
    new Blob([bytes as BlobPart], {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    }),
  );
  const a = document.createElement('a');
  a.href = url;
  a.download = title.toLowerCase().replace(/\s+/g, '-') + '.xlsx';
  a.click();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}
