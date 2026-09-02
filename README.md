# Municipal Services Application

A C# .NET Framework 4.7.2 Windows Forms application that allows South African citizens to engage with their municipality — reporting issues, tracking service requests, and staying informed about local events.

---

## Requirements

| Requirement | Version |
|---|---|
| Windows OS | Windows 7 or later |
| .NET Framework | 4.7.2 or later |
| Visual Studio | 2019 / 2022 / 2026 (any edition) |

---

## How to Compile

1. Open **Visual Studio**.
2. Select **File → Open → Project/Solution**.
3. Browse to the project folder and open **`Municipal App.slnx`**.
4. In the menu bar choose **Build → Build Solution** (or press **Ctrl + Shift + B**).
5. The compiled executable will be placed in:
   ```
   bin\Debug\Municipal App.exe
   ```

---

## How to Run

**From Visual Studio:**  
Press **F5** (with debugging) or **Ctrl + F5** (without debugging).

**From File Explorer:**  
Navigate to `bin\Debug\` and double-click **`Municipal App.exe`**.

---

## How to Use the Application

### Main Menu
On startup the application presents three service options:

| Option | Status |
|---|---|
| 📋 Report Issues | **Enabled — click to use** |
| 📅 Local Events & Announcements | Disabled (coming soon) |
| 🔧 Service Request Status | Disabled (coming soon) |

The footer displays your current **engagement level** and **points** earned through active participation.

---

### Reporting an Issue

1. Click **"Report Issues"** on the main menu.
2. Fill in all required fields:
   - **Location** — the street address or area where the issue is located.
   - **Category** — choose from the dropdown (Roads & Pavements, Water & Sanitation, Electricity, etc.).
   - **Description** — provide a detailed description (minimum 10 characters, up to 500).
3. Optionally attach supporting **images or documents** (JPG, PNG, PDF, DOCX):
   - Click **＋ Add File** to open a file browser.
   - Select one or more files.
   - Use **✕ Remove** to remove any selected file from the list.
4. Watch the **Form Completion** progress bar — it fills as you complete each required field.
5. Click **✔ Submit Report** to finalise your submission.
   - A confirmation dialog will show your **Reference Number**, category, timestamp, and the **10 engagement points** you earned.
6. Click **← Back to Main Menu** at any time to return without submitting.

---

## Engagement / Points System

Citizens earn **10 points** for every successfully submitted report.

| Level | Points Required |
|---|---|
| New Resident | 0–19 pts |
| Engaged Citizen 🥉 | 20–49 pts |
| Active Resident 🥈 | 50–99 pts |
| Community Champion 🏆 | 100+ pts |

Your current level and total points are always visible in the footer of the main menu.

---

## Project Structure

```
Municipal App/
├── Program.cs                  Entry point
├── MainMenuForm.cs             Main menu (startup form)
├── ReportIssueForm.cs          Report Issues form
├── Models/
│   └── IssueReport.cs          Data model + in-memory repository
├── Properties/
│   └── AssemblyInfo.cs
└── README.md                   This file
```

---

## Data Storage

All submitted reports are stored **in memory** for the duration of the application session (no external database required). Reports are accessible via `IssueReportRepository.GetAll()`.

---

## Design Principles Applied

- **Consistency** — uniform green/teal colour scheme (`RGB 0,128,96`) across all forms.
- **Clarity** — every field has a bold label and instructional hint text; buttons are clearly labelled.
- **User Feedback** — validation errors shown via `MessageBox` with `Warning` icon; successful submissions shown with `Information` icon including reference number.
- **Responsiveness** — all major controls use `Anchor` and `Dock` properties so the interface adapts to different window sizes and screen resolutions.
