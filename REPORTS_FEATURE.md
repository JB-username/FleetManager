# Odometer Reports Feature

## Overview
The Admin Reports page allows administrators to view and export odometer readings with photos in Excel format, with date range filtering.

## Features

### 1. Date Range Filtering
- **From Date**: Filter readings starting from this date (default: 30 days ago)
- **To Date**: Filter readings up to this date (default: today)
- Automatically applies when viewing the page or exporting

### 2. Report Display
The page displays a table with the following columns:
- **Captured Date**: When the reading was recorded
- **Vehicle Registration**: The vehicle's registration number
- **User Email**: The email of the user who logged the reading
- **User Name**: The user's full name
- **Odometer Reading**: The kilometer reading (formatted with thousands separator)
- **Photo**: Link to view the uploaded photo

### 3. Excel Export
Click "Download Excel" to generate an XLSX file containing:
- All readings for the selected date range
- **Embedded photos** with automatic scaling (30% size reduction)
- Formatted headers with bold text and light gray background
- Proper column widths for readability
- Row height adjustment for photo visibility
- Filename format: `odometer-report-YYYYMMDD-HHmmss.xlsx`

## File Structure
```
Pages/AdminDashboard/Reports/
├── Index.cshtml           # UI for reports and export
└── Index.cshtml.cs        # PageModel with export logic
```

## Page Model Details

### Properties
- `FromDate`: Start date for the date range
- `ToDate`: End date for the date range
- `Readings`: List of OdometerReadingRow objects to display

### Handler Methods
- `OnGetAsync()`: Loads readings for the current date range
- `OnPostExportAsync()`: Generates and returns the Excel file

### Helper Classes
- `OdometerReadingRow`: DTO containing:
  - Id, CapturedAt, VehicleRegistration
  - UserEmail, UserFullName, KilometerReading
  - ImagePath

## Database Integration
The feature uses:
- `OdometerReadings` table with relationships to Vehicles and Users
- `Vehicle` model (RegistrationNumber property)
- `ApplicationUser` model (FullName, Email properties)

## Security
- Page is protected with `[Authorize(Roles = "Admin")]`
- Only administrators can access the Reports page and export data
- Photo viewing via Serve endpoint validates ownership/admin role

## Dependencies
- **ClosedXML** (v0.104.0): Excel workbook creation and manipulation
- **Entity Framework Core**: Database queries with relationships
- **ASP.NET Core Identity**: User/role authorization

## Usage
1. Navigate to Admin Dashboard → Odometer Reports
2. Select a date range (optional - defaults to last 30 days)
3. Click "Filter" to view readings in the table
4. Click "Download Excel" to export with embedded photos
5. Click "View" link in Photo column to see individual photos

## Notes
- Photos are embedded in the Excel file if they exist on disk
- If a photo cannot be embedded, a status message is shown
- The Excel file preserves formatting and is ready for distribution
- Date ranges are normalized to UTC day boundaries for consistency

## Future Enhancements
- Add vehicle filter dropdown
- Add user filter dropdown
- Customize date range presets (Last 7 days, Last month, etc.)
- Add chart/graph visualization
- Schedule report generation and email delivery
