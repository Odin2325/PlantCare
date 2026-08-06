import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'plants',
    loadComponent: () =>
      import(
        './features/plant-catalog/pages/plant-catalog-page/plant-catalog-page'
      ).then(
        (component) => component.PlantCatalogPage,
      ),
  },
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'plants',
  },
  {
    path: '**',
    redirectTo: 'plants',
  },
];
