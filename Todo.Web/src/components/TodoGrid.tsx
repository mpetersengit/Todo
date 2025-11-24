import { useCallback, useEffect, useMemo, useState } from 'react';
import { AgGridReact } from 'ag-grid-react';
import { ModuleRegistry, AllCommunityModule } from 'ag-grid-community';
import type { ColDef, GridReadyEvent, ICellRendererParams } from 'ag-grid-community';
import 'ag-grid-community/styles/ag-grid.css';
import 'ag-grid-community/styles/ag-theme-alpine.css';
import { todoApi } from '../services/todoApi';
import type { TodoItem } from '../types/todo';
import TodoModal from './TodoModal';
import './TodoGrid.css';

// Register AG Grid modules
ModuleRegistry.registerModules([AllCommunityModule]);

interface ActionButtonsProps extends ICellRendererParams {
  onEdit: (todo: TodoItem) => void;
  onDelete: (id: string) => void;
  onComplete: (id: string, completed: boolean) => void;
}

const ActionButtons = (props: ActionButtonsProps) => {
  const todo = props.data as TodoItem;

  return (
    <div className="action-buttons">
      <button
        className="btn-edit"
        onClick={() => props.onEdit(todo)}
        title="Edit"
      >
        ✏️
      </button>
      {(todo.isCompleted ?? (todo as any).IsCompleted) ? (
        <button
          className="btn-incomplete"
          onClick={() => props.onComplete(todo.id, false)}
          title="Mark Incomplete"
        >
          ↩️
        </button>
      ) : (
        <button
          className="btn-complete"
          onClick={() => props.onComplete(todo.id, true)}
          title="Mark Complete"
        >
          ✓
        </button>
      )}
      <button
        className="btn-delete"
        onClick={() => props.onDelete(todo.id)}
        title="Delete"
      >
        🗑️
      </button>
    </div>
  );
};

const TodoGrid = () => {
  const [rowData, setRowData] = useState<TodoItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [modalOpen, setModalOpen] = useState(false);
  const [editingTodo, setEditingTodo] = useState<TodoItem | null>(null);
  const [filters, setFilters] = useState({ isCompleted: undefined as boolean | undefined });
  const [pagination, setPagination] = useState({ page: 1, pageSize: 10, totalCount: 0 });

  const handleEdit = useCallback((todo: TodoItem) => {
    setEditingTodo(todo);
    setModalOpen(true);
  }, []);

  const loadTodos = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      // Use server-side pagination
      const response = await todoApi.listTodos({
        page: pagination.page,
        pageSize: pagination.pageSize,
        isCompleted: filters.isCompleted,
      });
      setRowData(response.items);
      setPagination(prev => ({
        ...prev,
        totalCount: response.totalCount,
      }));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load todos');
      console.error('Error loading todos:', err);
    } finally {
      setLoading(false);
    }
  }, [pagination.page, pagination.pageSize, filters.isCompleted]);

  const handleDelete = useCallback(async (id: string) => {
    if (!confirm('Are you sure you want to delete this todo?')) return;
    
    try {
      await todoApi.deleteTodo(id);
      // Reload current page, or go to previous page if current page becomes empty
      await loadTodos();
      // If current page is empty and not first page, go to previous page
      if (rowData.length === 1 && pagination.page > 1) {
        setPagination(prev => ({ ...prev, page: prev.page - 1 }));
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete todo');
      console.error('Error deleting todo:', err);
    }
  }, [loadTodos, rowData.length, pagination.page]);

  const handleToggleComplete = useCallback(async (id: string, completed: boolean) => {
    try {
      if (completed) {
        await todoApi.completeTodo(id);
      } else {
        await todoApi.incompleteTodo(id);
      }
      await loadTodos();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update todo');
      console.error('Error updating todo:', err);
    }
  }, [loadTodos]);

  const columnDefs: ColDef[] = useMemo(() => [
    {
      field: 'title',
      headerName: 'Title',
      sortable: true,
      filter: 'agTextColumnFilter',
      flex: 2,
    },
    {
      field: 'description',
      headerName: 'Description',
      sortable: true,
      filter: 'agTextColumnFilter',
      flex: 2,
    },
    {
      field: 'dueDate',
      headerName: 'Due Date',
      sortable: true,
      filter: 'agTextColumnFilter',
      flex: 1,
      valueFormatter: (params) => params.value || '-',
    },
    {
      field: 'isCompleted',
      headerName: 'Status',
      sortable: true,
      filter: 'agTextColumnFilter',
      filterParams: {
        filterOptions: ['equals'],
        defaultOption: 'equals',
      },
      valueGetter: (params) => {
        // Handle both camelCase (isCompleted) and PascalCase (IsCompleted) for compatibility
        const isCompleted = params.data?.isCompleted ?? params.data?.IsCompleted ?? false;
        return isCompleted ? 'Completed' : 'Pending';
      },
      cellRenderer: (params: ICellRendererParams) => {
        const isCompleted = params.data?.isCompleted ?? params.data?.IsCompleted ?? false;
        return isCompleted ? '✅ Completed' : '⏳ Pending';
      },
    },
    {
      field: 'createdAt',
      headerName: 'Created',
      sortable: true,
      filter: 'agTextColumnFilter',
      flex: 1,
      valueFormatter: (params) => 
        params.value ? new Date(params.value).toLocaleDateString() : '-',
    },
    {
      headerName: 'Actions',
      cellRenderer: ActionButtons,
      cellRendererParams: {
        onEdit: handleEdit,
        onDelete: handleDelete,
        onComplete: handleToggleComplete,
      },
      sortable: false,
      filter: false,
      flex: 1,
      pinned: 'right',
    },
  ], [handleEdit, handleDelete, handleToggleComplete]);

  useEffect(() => {
    loadTodos();
  }, [loadTodos]);

  // Reset to page 1 when filter changes
  useEffect(() => {
    setPagination(prev => ({ ...prev, page: 1 }));
  }, [filters.isCompleted]);

  const handleAdd = useCallback(() => {
    setEditingTodo(null);
    setModalOpen(true);
  }, []);

  const handleSave = useCallback(async (todo: { title: string; description?: string; dueDate?: string; isCompleted?: boolean }) => {
    try {
      if (editingTodo) {
        await todoApi.updateTodo(editingTodo.id, todo);
      } else {
        await todoApi.createTodo(todo);
      }
      setModalOpen(false);
      setEditingTodo(null);
      await loadTodos();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save todo');
      console.error('Error saving todo:', err);
    }
  }, [editingTodo, loadTodos]);


  const onGridReady = useCallback((_params: GridReadyEvent) => {
    // Grid is ready
  }, []);

  const onPaginationChanged = useCallback(() => {
    // Server-side pagination handled via custom controls
  }, []);

  return (
    <div className="todo-grid-container">
      <div className="todo-header">
        <h1>Todo List</h1>
        <div className="header-actions">
          <select
            value={filters.isCompleted === undefined ? 'all' : String(filters.isCompleted)}
            onChange={(e) => setFilters({
              isCompleted: e.target.value === 'all' ? undefined : e.target.value === 'true'
            })}
            className="filter-select"
          >
            <option value="all">All</option>
            <option value="false">Pending</option>
            <option value="true">Completed</option>
          </select>
          <button className="btn-add" onClick={handleAdd}>
            + Add Todo
          </button>
        </div>
      </div>

      {error && (
        <div className="error-message">
          {error}
          <button onClick={() => setError(null)}>✕</button>
        </div>
      )}

      <div className="ag-theme-alpine" style={{ height: '600px', width: '100%' }}>
        <AgGridReact
          theme="legacy"
          rowData={rowData}
          columnDefs={columnDefs}
          onGridReady={onGridReady}
          onPaginationChanged={onPaginationChanged}
          pagination={false}
          defaultColDef={{
            resizable: true,
            sortable: false,
            filter: false,
          }}
          loading={loading}
          animateRows={true}
        />
      </div>
      {pagination.totalCount > 0 && (
        <div className="pagination-info">
          <div className="pagination-stats">
            Showing {((pagination.page - 1) * pagination.pageSize) + 1} to {Math.min(pagination.page * pagination.pageSize, pagination.totalCount)} of {pagination.totalCount} todos
          </div>
          <div className="pagination-controls">
            <button
              disabled={pagination.page === 1}
              onClick={() => setPagination(prev => ({ ...prev, page: prev.page - 1 }))}
            >
              Previous
            </button>
            <span>Page {pagination.page} of {Math.ceil(pagination.totalCount / pagination.pageSize)}</span>
            <select
              value={pagination.pageSize}
              onChange={(e) => setPagination(prev => ({ ...prev, pageSize: Number(e.target.value), page: 1 }))}
              className="page-size-select"
            >
              <option value={10}>10 per page</option>
              <option value={25}>25 per page</option>
              <option value={50}>50 per page</option>
              <option value={100}>100 per page</option>
            </select>
            <button
              disabled={pagination.page >= Math.ceil(pagination.totalCount / pagination.pageSize)}
              onClick={() => setPagination(prev => ({ ...prev, page: prev.page + 1 }))}
            >
              Next
            </button>
          </div>
        </div>
      )}

      {modalOpen && (
        <TodoModal
          todo={editingTodo}
          onSave={handleSave}
          onClose={() => {
            setModalOpen(false);
            setEditingTodo(null);
          }}
        />
      )}
    </div>
  );
};

export default TodoGrid;

