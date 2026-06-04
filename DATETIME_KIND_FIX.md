# PostgreSQL DateTime Kind Fix - Reports Filter

## Problem
When clicking the Filter button, you received this error:
```
System.ArgumentException: Cannot write DateTime with Kind=Unspecified to PostgreSQL type 'timestamp with time zone', 
only UTC is supported.
```

## Root Cause
- HTML `<input type="date">` returns a `DateTime` with `Kind=Unspecified`
- PostgreSQL requires all datetime values to have `Kind=Utc` when storing in `timestamp with time zone` columns
- When EF Core tried to execute the LINQ query with these Unspecified dates, PostgreSQL rejected them

## Solution
Added `NormalizeDateToUtc()` helper method that:

1. **Checks the DateTime Kind**:
   - If already `DateTimeKind.Utc` → returns unchanged
   - If `DateTimeKind.Unspecified` (from form input) → converts to UTC using `DateTime.SpecifyKind()`
   - If `DateTimeKind.Local` → converts to UTC using `ToUniversalTime()`

2. **Called in three places**:
   - `LoadReadingsAsync()` - for filter display
   - `OnPostExportAsync()` - for Excel export
   - Both FromDate and ToDate values are normalized

## Code Changes

### Before:
```csharp
private async Task LoadReadingsAsync()
{
    var fromDate = FromDate ?? DateTime.MinValue;
    var toDate = ToDate ?? DateTime.MaxValue;

    fromDate = fromDate.Date;
    toDate = toDate.Date.AddDays(1).AddTicks(-1);
    // ... query with potentially Unspecified DateTime values
}
```

### After:
```csharp
private async Task LoadReadingsAsync()
{
    var fromDate = NormalizeDateToUtc(FromDate ?? DateTime.MinValue);
    var toDate = NormalizeDateToUtc(ToDate ?? DateTime.MaxValue);

    if (ToDate.HasValue)
    {
        toDate = toDate.AddDays(1).AddTicks(-1);
    }
    // ... query with proper UTC DateTime values
}

private DateTime NormalizeDateToUtc(DateTime dateTime)
{
    if (dateTime.Kind == DateTimeKind.Utc)
        return dateTime;

    if (dateTime.Kind == DateTimeKind.Unspecified)
    {
        return DateTime.SpecifyKind(dateTime.Date, DateTimeKind.Utc);
    }

    return dateTime.ToUniversalTime();
}
```

## Why This Works

1. **DateTime.SpecifyKind()** - Marks an Unspecified DateTime as UTC without changing the value
   - Input: `2024-12-15` (Unspecified) → Output: `2024-12-15` (Utc)
   - Safe because we're treating form input as already in the user's local time

2. **PostgreSQL accepts UTC** - Now all DateTime values have `Kind=Utc`
   - Query executes successfully
   - Data is stored correctly in the database

3. **Preserves filter logic** - Date range boundaries remain the same
   - Start of selected day (00:00:00 UTC)
   - End of selected day (23:59:59.9999999 UTC)

## Testing

After this fix, the Filter button should:
1. ✅ Accept date selections from the form
2. ✅ Return records matching the date range
3. ✅ Not throw DateTime Kind conversion errors
4. ✅ Work with PostgreSQL's timestamp with time zone type

## Database Column Context

Your `OdometerReadings.CapturedAt` column is likely defined as:
```sql
"CapturedAt" timestamp with time zone NOT NULL
```

This column type in PostgreSQL:
- Stores timestamps in UTC internally
- Converts to/from client time zones
- **Requires** `DateTimeKind.Utc` for parameter binding

## Additional Notes

- This fix applies to all three filter handlers: Filter, Clear, and Export
- The default dates set on page load are already UTC (using `DateTime.UtcNow`)
- No migration needed - only the query parameters are affected
