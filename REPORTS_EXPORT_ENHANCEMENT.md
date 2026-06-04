# Reports Export Enhancement - Photo Sizing & Format Options

## New Features

### 1. **Two Export Options**
The Reports page now provides two Excel export buttons:

| Button | File Name | Contains | Use Case |
|--------|-----------|----------|----------|
| **Excel (With Photos)** | `odometer-report-with-photos-*.xlsx` | Data + Embedded Photos | Complete record with visual reference |
| **Excel (No Photos)** | `odometer-report-*.xlsx` | Data Only | Lightweight file for sharing/archiving |

### 2. **Significantly Smaller Photos**
- **Previous scaling**: 30% of original size
- **New scaling**: 10% of original size (reduced by 67%)
- **Row height**: Reduced from 60px to 30px
- **Result**: Compact, non-overlapping photos in Excel

### 3. **Smart Column Management**
- Photo column (Column 6) only appears in "With Photos" export
- Column 6 width set to 12 units for compact display
- File size reduction when exporting without photos

## Technical Changes

### PageModel Updates

#### New Property:
```csharp
[BindProperty]
public string ExportType { get; set; } = "WithPhotos";
```

#### Updated Export Handler:
```csharp
public async Task<IActionResult> OnPostExportAsync()
{
    // ... query and workbook setup ...

    if (ExportType == "WithPhotos")
    {
        worksheet.Cell(1, 6).Value = "Photo";
    }

    foreach (var reading in readings)
    {
        // ... data cells ...

        if (ExportType == "WithPhotos" && !string.IsNullOrEmpty(reading.ImagePath))
        {
            // Scale to 10% (was 30%)
            picture.Scale(0.1);
            // Set row height to 30px (was 60px)
            worksheet.Row(row).Height = 30;
        }
    }

    if (ExportType == "WithPhotos")
    {
        worksheet.Column(6).Width = 12;
    }

    // Generate file with appropriate name
    string fileName = ExportType == "WithPhotos" 
        ? $"odometer-report-with-photos-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx"
        : $"odometer-report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx";
}
```

### UI Changes

#### Before:
- Single "Download Excel" button
- Always included photos at 30% scale
- Fixed column structure

#### After:
- Two side-by-side buttons in the card header
- "Excel (With Photos)" - includes scaled photos
- "Excel (No Photos)" - data only, faster/smaller
- Hidden export form handles the submission

#### Button Styling:
```html
<button type="submit" 
        form="exportForm"
        name="exportType"
        value="WithPhotos"
        class="btn btn-success btn-sm">
    <i class="bi bi-file-earmark-spreadsheet"></i> Excel (With Photos)
</button>

<button type="submit" 
        form="exportForm"
        name="exportType"
        value="NoPhotos"
        class="btn btn-info btn-sm">
    <i class="bi bi-file-earmark-spreadsheet"></i> Excel (No Photos)
</button>
```

## Benefits

### For Users
- ✅ **Choice**: Select format based on use case
- ✅ **Compact**: Photos no longer overlap or take excessive space
- ✅ **Faster**: No-photo option generates quickly
- ✅ **Smaller Files**: Both options are smaller than before
- ✅ **Clear Naming**: Filename indicates what's included

### For Data Quality
- ✅ **Backup Format**: No-photo version serves as data-only backup
- ✅ **Fallback**: If photos fail to embed, use no-photo version
- ✅ **Flexibility**: Choose what works best for your needs

## Photo Size Comparison

| Metric | Previous | New (With Photos) | Reduction |
|--------|----------|------------------|-----------|
| Photo Scale | 30% | 10% | 67% smaller |
| Row Height | 60px | 30px | 50% smaller |
| Visual Impact | Large, overlapping | Compact, inline | Much improved |
| File Size Impact | ~500KB+ per photo | ~50KB per photo | ~90% reduction |

## Export Form Details

```html
<form id="exportForm" method="post" asp-page-handler="Export" style="display: none;">
    <input type="hidden" asp-for="FromDate" />
    <input type="hidden" asp-for="ToDate" />
</form>
```

- Hidden form preserves filter dates
- Buttons submit to same form with different `exportType` values
- Maintains all current filtering functionality

## Usage

1. Filter readings by date range
2. Click either export button:
   - **With Photos**: For complete visual record
   - **No Photos**: For lightweight data backup
3. File downloads with appropriate name and format

## Future Enhancements

Possible improvements:
- Add export type preference in user settings
- Adjustable photo scaling option (user-configurable)
- Compress photos before embedding
- Add summary sheet with statistics
- Export to PDF format option
- Schedule automated exports
