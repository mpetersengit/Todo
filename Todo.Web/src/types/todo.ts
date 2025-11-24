export interface TodoItem {
  id: string;
  title: string;
  description?: string;
  dueDate?: string;
  isCompleted: boolean;
  createdAt: string;
}

export interface CreateTodoRequest {
  title: string;
  description?: string;
  dueDate?: string;
}

export interface UpdateTodoRequest {
  title?: string;
  description?: string;
  dueDate?: string;
}

export interface PaginatedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface ListTodosQuery {
  isCompleted?: boolean;
  overdue?: boolean;
  dueBefore?: string;
  dueAfter?: string;
  sortBy?: 'Title' | 'DueDate' | 'CreatedAt';
  order?: 'Asc' | 'Desc';
  page?: number;
  pageSize?: number;
}

