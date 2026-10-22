import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { PlantGrowthReferences } from './plant-growth-references';

describe('Growth references', () => {
  it('omits the section for existing profiles without references', () => {
    const fixture = TestBed.createComponent(PlantGrowthReferences);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('section')).toBeNull();
    fixture.componentRef.setInput('profile', { careSourceUrl: 'https://example.org', reviewedOn: '2026-10-22', photo: null });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('section')).toBeNull();
  });
  it('shows stage descriptions, limitations and attribution even if a reference photo fails', () => {
    const fixture = TestBed.createComponent(PlantGrowthReferences);
    fixture.componentRef.setInput('profile', {
      careSourceUrl: 'https://example.org/care', reviewedOn: '2026-10-22', photo: null,
      growthReferences: [{ title: 'Root beginnings', description: 'Roots forming on a plantlet.', photo: {
        path: '/images/plants/example.jpg', altText: 'Small roots on a plantlet', credit: 'Photographer',
        sourceUrl: 'https://example.org/photo', licenseName: 'CC BY 4.0', licenseUrl: 'https://creativecommons.org/licenses/by/4.0/',
      } }],
    });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('img').alt).toBe('Small roots on a plantlet');
    expect(fixture.nativeElement.textContent).toContain('different specimens');
    expect(fixture.nativeElement.textContent).toContain('Roots forming on a plantlet.');
    fixture.nativeElement.querySelector('img').dispatchEvent(new Event('error')); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Photo unavailable');
    expect(fixture.nativeElement.querySelectorAll('a').length).toBe(3);
  });
});
