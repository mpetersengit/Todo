# Todo Web Interface

A React + TypeScript web application for managing todo items using AG Grid Community Edition. This frontend application provides a professional data grid interface for the Todo API backend.

## Features

- **AG Grid Integration**: Enterprise-grade data grid with built-in sorting, filtering, and pagination
- **Full CRUD Operations**: Create, read, update, and delete todo items
- **Status Management**: Mark todos as complete or incomplete with a single click
- **Advanced Filtering**: Filter by completion status with dropdown selector
- **Server-Side Pagination**: Efficient pagination handled by the API, configurable page sizes (10, 25, 50, 100 items per page)
- **Responsive Design**: Clean, minimal styling focused on functionality

## Technology Stack

- **React 19** with TypeScript
- **Vite 7** for build tooling and development server
- **AG Grid Community Edition** for data grid functionality
- **CSS** for styling

## Prerequisites

- Node.js 20.19+ or 22.12+ (required for Vite 7)
- npm (comes with Node.js)
- Todo API running on `http://localhost:5245`

## Getting Started

### Installation

```bash
cd Todo.Web
npm install
```

### Development

```bash
npm run dev
```

The application will be available at `http://localhost:5173` (or the port Vite assigns).

### Build

```bash
npm run build
```

Production-ready files will be generated in the `dist/` directory.

## Configuration

The API URL can be configured via environment variable:

- Create a `.env` file with `VITE_API_URL=http://localhost:5245`
- Or set it when running: `VITE_API_URL=http://localhost:8080 npm run dev`

By default, the Vite dev server proxies `/todos` requests to `http://localhost:5245`.

## Usage

1. **View Todos**: The grid displays all todos with columns for title, description, due date, status, and created date
2. **Add Todo**: Click the "+ Add Todo" button to open the creation modal
3. **Edit Todo**: Click the ✏️ edit button in the Actions column
4. **Complete/Incomplete**: Click ✓ to mark complete or ↩️ to mark incomplete
5. **Delete**: Click 🗑️ to delete a todo (with confirmation dialog)
6. **Filter**: Use the dropdown to filter by completion status (All, Pending, Completed)
7. **Paginate**: Use the pagination controls at the bottom to navigate pages and change page size

## Project Structure

```
Todo.Web/
├── src/
│   ├── components/
│   │   ├── TodoGrid.tsx      # Main grid component with AG Grid
│   │   ├── TodoGrid.css      # Grid styling
│   │   ├── TodoModal.tsx     # Add/Edit modal component
│   │   └── TodoModal.css     # Modal styling
│   ├── services/
│   │   └── todoApi.ts        # API service layer
│   ├── types/
│   │   └── todo.ts           # TypeScript interfaces
│   ├── App.tsx               # Root component
│   └── main.tsx              # Application entry point
├── public/                   # Static assets
├── vite.config.ts            # Vite configuration
└── package.json              # Dependencies and scripts
```

## API Integration

The application communicates with the Todo API backend via REST endpoints:

- `GET /todos` - List todos with filtering, sorting, and pagination
- `GET /todos/{id}` - Get a specific todo
- `POST /todos` - Create a new todo
- `PUT /todos/{id}` - Update an existing todo
- `PATCH /todos/{id}/complete` - Mark todo as complete
- `PATCH /todos/{id}/incomplete` - Mark todo as incomplete
- `DELETE /todos/{id}` - Delete a todo

All API calls are handled through the `todoApi` service layer with proper error handling and loading states.
