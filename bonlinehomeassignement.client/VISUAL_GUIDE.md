# Admin Dashboard - Visual Guide & Page Layouts

## Homepage Flow
```
User visits: http://localhost:3000
					↓
			App.jsx checks route
					↓
			"/" → Redirect to "/dashboard"
					↓
			Dashboard Page Loads
					↓
		Shows Statistics & Welcome
```

---

## 📊 Dashboard Page Layout

```
┌─────────────────────────────────────────────────────────┐
│  📊 Patient Care Admin  │  Dashboard  Leads  Programs  Tenants  │
├─────────────────────────────────────────────────────────┤
│                                                           │
│  Admin Dashboard                                          │
│  Welcome to the Patient Care Management System           │
│                                                           │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │
│  │ 👥          │  │ 📋          │  │ 🏢          │   │
│  │    2        │  │    2        │  │    2        │   │
│  │ Total Leads │  │ Programs    │  │ Tenants     │   │
│  └──────────────┘  └──────────────┘  └──────────────┘   │
│                                                           │
│  Quick Start Guide                                       │
│  • Leads: Manage potential patients and prospects       │
│  • Programs: View and manage care programs              │
│  • Tenants: Manage system tenants and configurations   │
│                                                           │
└─────────────────────────────────────────────────────────┘
```

---

## 👥 Leads Dashboard Layout

```
┌─────────────────────────────────────────────────────────┐
│  📊 Patient Care Admin  │  Dashboard  Leads  Programs  Tenants  │
├─────────────────────────────────────────────────────────┤
│                                                           │
│  👥 Leads Management              [+ Add New Lead]      │
│                                                           │
│  ┌─────────────────────────────────────────────────────┐ │
│  │  Name   │   Email    │  Phone   │  Source  │Actions│ │
│  ├─────────────────────────────────────────────────────┤ │
│  │ John    │ john@...   │ 555-001  │Website  │Edit Del│ │
│  │ Jane    │ jane@...   │ 555-002  │Referral │Edit Del│ │
│  └─────────────────────────────────────────────────────┘ │
│                                                           │
│  [Form appears when "Add New Lead" is clicked]           │
│                                                           │
└─────────────────────────────────────────────────────────┘
```

---

## 📋 Programs Dashboard Layout

```
┌─────────────────────────────────────────────────────────┐
│  📊 Patient Care Admin  │  Dashboard  Leads  Programs  Tenants  │
├─────────────────────────────────────────────────────────┤
│                                                           │
│  📋 Programs Management         [+ Add New Program]     │
│                                                           │
│  ┌─────────────────────────────────────────────────────┐ │
│  │  Name  │ Description │ Start Date │Status│ Actions │ │
│  ├─────────────────────────────────────────────────────┤ │
│  │ Basic  │ Entry-level │ 2026-09-09 │Active│Edit Del│ │
│  │ Adv    │ Complex     │ 2026-09-09 │Active│Edit Del│ │
│  └─────────────────────────────────────────────────────┘ │
│                                                           │
│  [Form appears when "Add New Program" is clicked]        │
│                                                           │
└─────────────────────────────────────────────────────────┘
```

---

## 🏢 Tenants Dashboard Layout

```
┌─────────────────────────────────────────────────────────┐
│  📊 Patient Care Admin  │  Dashboard  Leads  Programs  Tenants  │
├─────────────────────────────────────────────────────────┤
│                                                           │
│  🏢 Tenants Management          [+ Add New Tenant]      │
│                                                           │
│  ┌──────────────────────────────────────────────────────┐ │
│  │  Tenant Name │ Hostname   │ Created  │   Actions    │ │
│  ├──────────────────────────────────────────────────────┤ │
│  │ Default T.   │ localhost  │ 09/09/26 │ Edit  Delete │ │
│  │ Provider Inc │ acme.local │ 09/09/26 │ Edit  Delete │ │
│  └──────────────────────────────────────────────────────┘ │
│                                                           │
│  ┌──────────────────────────────────────────────────────┐ │
│  │  📌 About Tenants                                    │ │
│  │  Tenants represent separate customer organizations  │ │
│  │  in the multi-tenant system...                       │ │
│  └──────────────────────────────────────────────────────┘ │
│                                                           │
└─────────────────────────────────────────────────────────┘
```

---

## 🎨 Form Modal Layout (for Add/Edit)

```
┌─────────────────────────────────┐
│  New Lead              ✕        │
├─────────────────────────────────┤
│                                 │
│  Name *                         │
│  [________________]             │
│                                 │
│  Email *                        │
│  [________________]             │
│                                 │
│  Phone                          │
│  [________________]             │
│                                 │
│  Source                         │
│  [Select a source...    ▼]      │
│                                 │
│  [Create Lead]  [Cancel]        │
│                                 │
└─────────────────────────────────┘
```

---

## 🎯 User Interaction Flow

### Adding a New Record

```
1. Click "[+ Add New ___]" Button
		↓
2. Form Modal Appears
		↓
3. User Fills Fields
		↓
4. Click Submit Button
		↓
5. API Call (or Mock Data)
		↓
6. Table Updates
		↓
7. Form Closes
		↓
8. Success Message Shown
```

### Editing a Record

```
1. Click "Edit" Button in Table Row
		↓
2. Form Modal Appears (Pre-filled with Data)
		↓
3. User Modifies Fields
		↓
4. Click "Update ___" Button
		↓
5. API Call (or Mock Data)
		↓
6. Table Updates
		↓
7. Form Closes
```

### Deleting a Record

```
1. Click "Delete" Button in Table Row
		↓
2. Confirmation Dialog Appears
   "Are you sure you want to delete this ___?"
		↓
3. Click "OK" to Confirm
		↓
4. API Call (or Mock Data)
		↓
5. Record Removed from Table
```

---

## 📱 Responsive Design

### Desktop View (1200px+)
- Full navigation bar
- Multi-column tables
- 3-column stats grid
- Full form width

### Tablet View (768px - 1199px)
- Stacked navigation
- 2-column tables (some)
- 2-column stats grid
- Adjusted form width

### Mobile View (<768px)
- Vertically stacked navigation
- 1-column tables (horizontal scroll)
- 1-column stats grid
- Full-width forms

---

## 🎨 Color Reference

| Color | Hex | Usage |
|-------|-----|-------|
| Primary Blue | #0d6efd | Buttons, Links |
| Success Green | #198754 | Active status, Approve |
| Danger Red | #dc3545 | Delete, Error |
| Info Cyan | #0dcaf0 | Info buttons |
| Secondary Gray | #6c757d | Inactive status |
| Background | #f8f9fa | Page background |
| White | #ffffff | Cards, Modals |

---

## ⌨️ Keyboard Navigation

- **Tab** - Move between form fields
- **Enter** - Submit form
- **Esc** - Close modal
- **Tab + Shift** - Move between fields backward

---

## ♿ Accessibility Features

✅ Semantic HTML
✅ ARIA labels on form inputs
✅ Keyboard navigation support
✅ Color contrast meets WCAG AA
✅ Focus indicators visible
✅ Form validation messages
✅ Error alerts announce to screen readers

---

## 🚀 Next Time You Start the App

1. Open `/bonlinehomeassignement.client` directory
2. Run: `npm run dev`
3. Browser opens automatically to `http://localhost:3000`
4. You'll see the Dashboard homepage
5. Navigation works - click any menu item to navigate
6. All pages have mock data to show you what it looks like

**No forecast data, no old AppointmentManager - just your clean admin dashboard!** ✨
