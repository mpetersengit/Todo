import type { TodoItem, CreateTodoRequest, UpdateTodoRequest, PaginatedResponse, ListTodosQuery } from '../types/todo';

// Use relative URL in dev (for Vite proxy) or explicit URL if set
const API_BASE_URL = import.meta.env.VITE_API_URL || '';

class TodoApiService {
  private async request<T>(endpoint: string, options?: RequestInit): Promise<T> {
    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
      headers: {
        'Content-Type': 'application/json',
        ...options?.headers,
      },
      ...options,
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: response.statusText }));
      throw new Error(error.message || `HTTP error! status: ${response.status}`);
    }

    if (response.status === 204) {
      return {} as T;
    }

    return response.json();
  }

  async listTodos(query: ListTodosQuery = {}): Promise<PaginatedResponse<TodoItem>> {
    const params = new URLSearchParams();
    
    if (query.isCompleted !== undefined) params.append('isCompleted', String(query.isCompleted));
    if (query.overdue !== undefined) params.append('overdue', String(query.overdue));
    if (query.dueBefore) params.append('dueBefore', query.dueBefore);
    if (query.dueAfter) params.append('dueAfter', query.dueAfter);
    if (query.sortBy) params.append('sortBy', query.sortBy);
    if (query.order) params.append('order', query.order);
    if (query.page !== undefined) params.append('page', String(query.page));
    if (query.pageSize !== undefined) params.append('pageSize', String(query.pageSize));

    const queryString = params.toString();
    return this.request<PaginatedResponse<TodoItem>>(`/todos${queryString ? `?${queryString}` : ''}`);
  }

  async getTodo(id: string): Promise<TodoItem> {
    return this.request<TodoItem>(`/todos/${id}`);
  }

  async createTodo(todo: CreateTodoRequest): Promise<TodoItem> {
    return this.request<TodoItem>('/todos', {
      method: 'POST',
      body: JSON.stringify(todo),
    });
  }

  async updateTodo(id: string, todo: UpdateTodoRequest): Promise<TodoItem> {
    return this.request<TodoItem>(`/todos/${id}`, {
      method: 'PUT',
      body: JSON.stringify(todo),
    });
  }

  async completeTodo(id: string): Promise<TodoItem> {
    return this.request<TodoItem>(`/todos/${id}/complete`, {
      method: 'PATCH',
    });
  }

  async incompleteTodo(id: string): Promise<TodoItem> {
    return this.request<TodoItem>(`/todos/${id}/incomplete`, {
      method: 'PATCH',
    });
  }

  async deleteTodo(id: string): Promise<void> {
    return this.request<void>(`/todos/${id}`, {
      method: 'DELETE',
    });
  }
}

export const todoApi = new TodoApiService();

