import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import {
  beforeEach,
  describe,
  expect,
  it,
} from 'vitest';

import { App } from './app';

describe('App', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [
        App,
      ],
      providers: [
        provideRouter([]),
      ],
    });
  });

  it('should create the application', () => {
    const fixture = TestBed.createComponent(App);

    expect(fixture.componentInstance).toBeTruthy();
  });
});
