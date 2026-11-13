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
| 📅 Local Events & Announcements | **Enabled — click to use** |
| 🔧 Service Request Status | **Enabled — click to use** |

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

### Checking Service Request Status

1. Click **"Service Request Status"** on the main menu.
2. Browse all submitted service requests in the central list, or use the **Categories** tree on the left to drill down (e.g. Water & Sanitation → its requests).
3. **Track by reference #** — type a reference number (e.g. `SR-1006`) and click **Track** to instantly locate a request and view its full details (binary search tree lookup).
4. **Sort** the request list via the dropdown:
   - *Default* — insertion order.
   - *By Priority (AVL Tree)* — Critical → High → Medium → Low, served from a self-balancing AVL tree.
   - *By Date (Red-Black Tree)* — chronological order, served from a self-balancing red-black tree.
5. The right-hand sidebar always shows:
   - **Most Urgent (Heap)** — the most urgent open requests, ranked by a priority-queue (binary min-heap) combining priority and days open.
   - **Related Requests (Graph BFS)** — requests related to the one currently selected (same category/location), found via a hop- and weight-limited breadth-first search of the request relationship graph.
   - **Status Workflow (Binary Tree)** — the selected request's current status and its valid next steps, looked up in a binary tree of status transitions.
6. Click **🕸 Service Network** to open a visual map of how all requests relate to one another — greyed lines show every relationship, bold orange lines highlight the **Minimum Spanning Tree** (computed with Prim's algorithm) connecting every request with the least total "distance".
7. Double-click any request (in the list or a sidebar) for its full detail dialog.

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
├── Program.cs                        Entry point
├── MainMenuForm.cs                   Main menu (startup form)
├── ReportIssueForm.cs                Report Issues form
├── ViewReportsForm.cs                View submitted reports form
├── LocalEventsForm.cs                Local Events & Announcements form
├── ServiceRequestStatusForm.cs       Service Request Status form
├── ServiceNetworkForm.cs             Service request relationship graph / MST viewer
├── Models/
│   ├── IssueReport.cs                Data model + in-memory repository
│   ├── EventItem.cs                  Local event data model
│   ├── LocalEventRepository.cs       Local events repository (sorted dictionary, sets, search, recommendations)
│   ├── SimplePriorityQueue.cs        Generic binary min-heap (priority queue)
│   ├── ServiceRequest.cs             Service request data model + Priority/Status enums
│   ├── ServiceRequestRepository.cs   Service request repository (trees, graph, heap queries)
│   ├── Graph.cs                      Generic adjacency-list graph (BFS/DFS/Prim's MST)
│   └── Trees/
│       ├── BinarySearchTree.cs       Generic BST — lookup by reference number
│       ├── AvlTree.cs                Self-balancing AVL tree — sort by priority
│       ├── RedBlackTree.cs           Self-balancing red-black tree — sort by date
│       └── StatusWorkflowTree.cs     Binary tree of valid status transitions
├── Properties/
│   └── AssemblyInfo.cs
└── README.md                         This file
```

---

## Data Structures & Algorithms

The **Service Request Status** feature was built specifically to demonstrate a range of classic data structures and algorithms, each mapped to a genuine feature rather than a throwaway demo:

| Structure | Where it's used |
|---|---|
| **Basic Tree** | The left-hand `TreeView` control organising requests by **Category**. |
| **Binary Tree** | `StatusWorkflowTree` — models valid status transitions (e.g. `InProgress → Scheduled / Resolved`), shown in the "Status Workflow" sidebar. |
| **Binary Search Tree** | `BinarySearchTree<string, ServiceRequest>` keyed by reference number — powers **Track by reference #**. |
| **AVL Tree** | `AvlTree<PriorityKey, ServiceRequest>` — self-balancing tree keyed by priority, powers **Sort → By Priority**. |
| **Red-Black Tree** | `RedBlackTree<DateKey, ServiceRequest>` — self-balancing tree keyed by submission date, powers **Sort → By Date**. |
| **Heap / Priority Queue** | `SimplePriorityQueue<T>` (binary min-heap, shared with the Local Events feature) — ranks the **Most Urgent** requests and selects the cheapest edge in Prim's algorithm. |
| **Graph + Traversal** | `Graph<T>` adjacency list connecting related requests (shared category/location); a hop- and weight-limited breadth-first search powers **Related Requests**. |
| **Minimum Spanning Tree** | `Graph<T>.MinimumSpanningTree()` (Prim's algorithm) — visualised in the **Service Network** dialog as bold orange edges over the full relationship graph. |

Local Events & Announcements additionally uses a **sorted dictionary** (events ordered by date), **hash sets** (unique categories/dates), and a **priority queue** (date-earliest-first event ordering), plus a search-history-driven recommendation algorithm.

---

## Data Storage

All submitted reports are stored **in memory** for the duration of the application session (no external database required). Reports are accessible via `IssueReportRepository.GetAll()`.

---

## Design Principles Applied

- **Consistency** — uniform green/teal colour scheme (`RGB 0,128,96`) across all forms.
- **Clarity** — every field has a bold label and instructional hint text; buttons are clearly labelled.
- **User Feedback** — validation errors shown via `MessageBox` with `Warning` icon; successful submissions shown with `Information` icon including reference number.
- **Responsiveness** — all major controls use `Anchor` and `Dock` properties so the interface adapts to different window sizes and screen resolutions.
