namespace Nyar.Tests.Binary;

public class XlsEncoderDecoderRoundTripTests
{
    [Fact]
    public void RoundTrip_EmptyWorkbook()
    {
        var original = new ExcelWorkbookData
        {
            Sheets = []
        };

        var encoder = new XlsEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new XlsDecoder(bytes);
        var decoded = decoder.Decode();

        Assert.Empty(decoded.Sheets);
    }

    [Fact]
    public void RoundTrip_SingleSheetWorkbook()
    {
        var original = new ExcelWorkbookData
        {
            Sheets =
            [
                new ExcelSheetData
                {
                    Name = "Sheet1",
                    RowCount = 0,
                    ColumnCount = 0,
                    Rows = []
                }
            ]
        };

        var encoder = new XlsEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new XlsDecoder(bytes);
        var decoded = decoder.Decode();

        Assert.Single(decoded.Sheets);
        Assert.Equal("Sheet1", decoded.Sheets[0].Name);
    }

    [Fact]
    public void RoundTrip_MultipleSheetWorkbook()
    {
        var original = new ExcelWorkbookData
        {
            Sheets =
            [
                new ExcelSheetData { Name = "����", RowCount = 0, ColumnCount = 0, Rows = [] },
                new ExcelSheetData { Name = "����", RowCount = 0, ColumnCount = 0, Rows = [] },
                new ExcelSheetData { Name = "Summary", RowCount = 0, ColumnCount = 0, Rows = [] }
            ]
        };

        var encoder = new XlsEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new XlsDecoder(bytes);
        var decoded = decoder.Decode();

        Assert.Equal(3, decoded.Sheets.Count);
        Assert.Equal("����", decoded.Sheets[0].Name);
        Assert.Equal("����", decoded.Sheets[1].Name);
        Assert.Equal("Summary", decoded.Sheets[2].Name);
    }

    [Fact]
    public void RoundTrip_UnicodeSheetNames()
    {
        var original = new ExcelWorkbookData
        {
            Sheets =
            [
                new ExcelSheetData { Name = "��ɫ����", RowCount = 0, ColumnCount = 0, Rows = [] },
                new ExcelSheetData { Name = "�����ƥ�", RowCount = 0, ColumnCount = 0, Rows = [] }
            ]
        };

        var encoder = new XlsEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new XlsDecoder(bytes);
        var decoded = decoder.Decode();

        Assert.Equal(2, decoded.Sheets.Count);
        Assert.Equal("��ɫ����", decoded.Sheets[0].Name);
        Assert.Equal("�����ƥ�", decoded.Sheets[1].Name);
    }
}

public class XlsEncoderOutputTests
{
        [Fact]
        public void Encode_ProducesNonEmptyOutput()
        {
            var data = new ExcelWorkbookData
            {
                Sheets =
                [
                    new ExcelSheetData { Name = "Test", RowCount = 0, ColumnCount = 0, Rows = [] }
                ]
            };

            var encoder = new XlsEncoder();
            var bytes = encoder.Encode(data);

            Assert.True(bytes.Length > 0);
        }

        [Fact]
        public void Encode_StartsWithBofRecord()
        {
            var data = new ExcelWorkbookData { Sheets = [] };

            var encoder = new XlsEncoder();
            var bytes = encoder.Encode(data);

            Assert.True(bytes.Length >= 2);
            Assert.Equal(0x09, bytes[0]);
            Assert.Equal(0x08, bytes[1]);
        }

        [Fact]
        public void Encode_EndsWithEofRecord()
        {
            var data = new ExcelWorkbookData { Sheets = [] };

            var encoder = new XlsEncoder();
            var bytes = encoder.Encode(data);

            Assert.True(bytes.Length >= 4);
            var lastRecordType = (ushort)(bytes[^4] | (bytes[^3] << 8));
            Assert.Equal((ushort)0x000A, lastRecordType);
        }

        [Fact]
        public void Encode_ContainsBoundSheetRecord()
        {
            var data = new ExcelWorkbookData
            {
                Sheets =
                [
                    new ExcelSheetData { Name = "Sheet1", RowCount = 0, ColumnCount = 0, Rows = [] }
                ]
            };

            var encoder = new XlsEncoder();
            var bytes = encoder.Encode(data);

            var found = false;
            var reader = new ByteBuffer(bytes);

            while (!reader.IsEnd && reader.Remaining >= 4)
            {
                var recordType = reader.ReadU16LE();
                var recordLength = reader.ReadU16LE();

                if (recordType == 0x0085)
                {
                    found = true;
                    break;
                }

                if (reader.Remaining < recordLength)
                {
                    break;
                }

                reader.Advance(recordLength);
            }

            Assert.True(found);
        }
    }

    public class XlsScannerTests
    {
        private static byte[] build_minimal_xls(params string[] sheetNames)
        {
            var data = new ExcelWorkbookData
            {
                Sheets = sheetNames.Select(name => new ExcelSheetData
                {
                    Name = name,
                    RowCount = 0,
                    ColumnCount = 0,
                    Rows = []
                }).ToList()
            };

            var encoder = new XlsEncoder();
            return encoder.Encode(data);
        }

        [Fact]
        public void ValidateHeader_MinimalXls_ReturnsFalse()
        {
            var bytes = build_minimal_xls("Sheet1");
            var scanner = new XlsScanner(bytes);

            Assert.False(scanner.ValidateHeader());
        }

        [Fact]
        public void ValidateHeader_Ole2Magic_ReturnsTrue()
        {
            var ole2Header = new byte[512];
            var magic = OfficeConstants.Ole2MagicNumber;
            magic.CopyTo(ole2Header);
            var scanner = new XlsScanner(ole2Header);

            Assert.True(scanner.ValidateHeader());
        }

        [Fact]
        public void ScanStatistics_NonOle2Data_ReturnsEmptyStats()
        {
            var bytes = build_minimal_xls("Sheet1");
            var scanner = new XlsScanner(bytes);

            var stats = scanner.ScanStatistics();

            Assert.Equal(0, stats.SheetCount);
            Assert.Equal(0, stats.RowCount);
            Assert.Equal(0, stats.NumericCellCount);
            Assert.Equal(0, stats.StringCellCount);
            Assert.Equal(0, stats.FormulaCount);
        }
    }

    public class OfficeConstantsTests
    {
        [Fact]
        public void Ole2MagicNumber_CorrectLength()
        {
            Assert.Equal(8, OfficeConstants.Ole2MagicNumber.Length);
        }

        [Fact]
        public void Ole2MagicNumber_CorrectValues()
        {
            Assert.Equal(0xD0, OfficeConstants.Ole2MagicNumber[0]);
            Assert.Equal(0xCF, OfficeConstants.Ole2MagicNumber[1]);
            Assert.Equal(0x11, OfficeConstants.Ole2MagicNumber[2]);
            Assert.Equal(0xE0, OfficeConstants.Ole2MagicNumber[3]);
            Assert.Equal(0xA1, OfficeConstants.Ole2MagicNumber[4]);
            Assert.Equal(0xB1, OfficeConstants.Ole2MagicNumber[5]);
            Assert.Equal(0x1A, OfficeConstants.Ole2MagicNumber[6]);
            Assert.Equal(0xE1, OfficeConstants.Ole2MagicNumber[7]);
        }

        [Fact]
        public void XlsRecordType_Bof8_Is0x0809()
        {
            Assert.Equal((ushort)0x0809, OfficeConstants.XlsRecordType.Bof8);
        }

        [Fact]
        public void XlsRecordType_Eof_Is0x000A()
        {
            Assert.Equal((ushort)0x000A, OfficeConstants.XlsRecordType.Eof);
        }

        [Fact]
        public void XlsRecordType_BoundSheet_Is0x0085()
        {
            Assert.Equal((ushort)0x0085, OfficeConstants.XlsRecordType.BoundSheet);
        }
    }

    public class OfficeDataModelTests
    {
        [Fact]
        public void ExcelWorkbookData_DefaultSheets_IsEmpty()
        {
            var data = new ExcelWorkbookData();
            Assert.Empty(data.Sheets);
        }

        [Fact]
        public void ExcelSheetData_DefaultRows_IsEmpty()
        {
            var sheet = new ExcelSheetData();
            Assert.Empty(sheet.Rows);
            Assert.Equal(string.Empty, sheet.Name);
            Assert.Equal(0, sheet.RowCount);
            Assert.Equal(0, sheet.ColumnCount);
        }

        [Fact]
        public void ExcelCellData_Properties()
        {
            var cell = new ExcelCellData
            {
                Row = 2,
                Column = 3,
                Value = "Hello",
                CellReference = "D3"
            };

            Assert.Equal(2, cell.Row);
            Assert.Equal(3, cell.Column);
            Assert.Equal("Hello", cell.Value);
            Assert.Equal("D3", cell.CellReference);
        }

        [Fact]
        public void WordDocumentData_Defaults()
        {
            var doc = new WordDocumentData();
            Assert.Equal(string.Empty, doc.Text);
            Assert.Empty(doc.Paragraphs);
        }

        [Fact]
        public void PowerPointData_Defaults()
        {
            var ppt = new PowerPointData();
            Assert.Empty(ppt.Slides);
            Assert.Equal(0, ppt.SlideCount);
        }
    }

    public class XlsBiffRecordStructureTests
    {
        [Fact]
        public void BofRecord_HasCorrectStructure()
        {
            var data = new ExcelWorkbookData { Sheets = [] };
            var encoder = new XlsEncoder();
            var bytes = encoder.Encode(data);

            var reader = new ByteBuffer(bytes);
            var recordType = reader.ReadU16LE();
            var recordLength = reader.ReadU16LE();

            Assert.Equal((ushort)0x0809, recordType);
            Assert.Equal((ushort)16, recordLength);

            var biffVersion = reader.ReadU16LE();
            Assert.Equal((ushort)0x0600, biffVersion);
        }

        [Fact]
        public void MultipleSheets_ProduceMultipleBoundSheetRecords()
        {
            var data = new ExcelWorkbookData
            {
                Sheets =
                [
                    new ExcelSheetData { Name = "A", RowCount = 0, ColumnCount = 0, Rows = [] },
                    new ExcelSheetData { Name = "B", RowCount = 0, ColumnCount = 0, Rows = [] },
                    new ExcelSheetData { Name = "C", RowCount = 0, ColumnCount = 0, Rows = [] }
                ]
            };

            var encoder = new XlsEncoder();
            var bytes = encoder.Encode(data);

            var boundSheetCount = 0;
            var reader = new ByteBuffer(bytes);

            while (!reader.IsEnd && reader.Remaining >= 4)
            {
                var recordType = reader.ReadU16LE();
                var recordLength = reader.ReadU16LE();

                if (recordType == 0x0085)
                {
                    boundSheetCount++;
                }

                if (reader.Remaining < recordLength)
                {
                    break;
                }

                reader.Advance(recordLength);
            }

            Assert.Equal(3, boundSheetCount);
        }
    }

