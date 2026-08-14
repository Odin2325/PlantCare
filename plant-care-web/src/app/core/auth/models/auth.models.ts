export interface CurrentUser {
  id: string;
  email: string;
}

export interface Credentials {
  email: string;
  password: string;
}

export interface ValidationProblemDetails {
  title?: string;
  status?: number;
  errors?: Record<string, string[]>;
}
