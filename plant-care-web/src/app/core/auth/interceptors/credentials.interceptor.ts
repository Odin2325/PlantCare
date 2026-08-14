import { HttpInterceptorFn } from '@angular/common/http';

export const credentialsInterceptor: HttpInterceptorFn = (
  request,
  next,
) => {
  if (!request.url.startsWith('/api/')) {
    return next(request);
  }

  return next(
    request.clone({
      withCredentials: true,
    }),
  );
};
