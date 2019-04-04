namespace Nyar.Tests.Binary;

public sealed class OfficeRoundTripTests
{
    // -region-
    [Fact]
    public void EncodeDecode_SingleSheet_Roundtrip()
    {
        var original = new ExcelWorkbookData
        {
            Sheets = new List<ExcelSheetData>
            {
                new()
                {
                    Name = "Sheet1",
                    RowCount = 0,
                    ColumnCount = 0,
                    Rows = []
                }
            }
        };

        var encoder = new XlsEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new XlsDecoder(bytes);
        var result = decoder.Decode();

        Assert.Single(result.Sheets);
        Assert.Equal("Sheet1", result.Sheets[0].Name);
    }

    [Fact]
    public void EncodeDecode_MultipleSheets_Roundtrip()
    {
        var original = new ExcelWorkbookData
        {
            Sheets = new List<ExcelSheetData>
            {
                new() { Name = "Revenue", RowCount = 0, ColumnCount = 0, Rows = [] },
                new() { Name = "Expenses", RowCount = 0, ColumnCount = 0, Rows = [] },
                new() { Name = "Summary", RowCount = 0, ColumnCount = 0, Rows = [] }
            }
        };

        var encoder = new XlsEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new XlsDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(3, result.Sheets.Count);
        Assert.Equal("Revenue", result.Sheets[0].Name);
        Assert.Equal("Expenses", result.Sheets[1].Name);
        Assert.Equal("Summary", result.Sheets[2].Name);
    }

    [Fact]
    public void EncodeDecode_EmptySheets_Roundtrip()
    {
        var original = new ExcelWorkbookData
        {
            Sheets = []
        };

        var encoder = new XlsEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new XlsDecoder(bytes);
        var result = decoder.Decode();

        Assert.Empty(result.Sheets);
    }

    [Fact]
    public void EncodeDecode_ChineseName_Roundtrip()
    {
        var original = new ExcelWorkbookData
        {
            Sheets = new List<ExcelSheetData>
            {
                new()
                {
                    Name = "员工�?,
                    RowCount = 0,
                    ColumnCount = 0,
                    Rows = []
                },
                new()
                {
                    Name = "工资�?,
                    RowCount = 0,
                    ColumnCount = 0,
                    Rows = []
                }
            }
        };

        var encoder = new XlsEncoder();
        var bytes = encoder.Encode(original);
        var decoder = new XlsDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(2, result.Sheets.Count);
        Assert.Equal("员工�?, result.Sheets[0].Name);
        Assert.Equal("工资�?, result.Sheets[1].Name);
    }

    // [/section renamed - encoding fix]
}

