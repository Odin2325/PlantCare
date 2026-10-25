import { TestBed } from '@angular/core/testing';
import { afterEach, describe, expect, it } from 'vitest';
import { PlantGrowingGuide } from './plant-growing-guide';

describe('PlantGrowingGuide', () => {
  afterEach(() => TestBed.resetTestingModule());
  it('hides the guide for older profiles without garden guidance', () => {
    const fixture = TestBed.createComponent(PlantGrowingGuide);
    fixture.detectChanges(); expect(fixture.nativeElement.querySelector('section')).toBeNull();
  });
  it('renders sourced cultivar guidance as text and explains weather and edible-part limits', () => {
    const fixture = TestBed.createComponent(PlantGrowingGuide);
    fixture.componentRef.setInput('profile', { categories: ['fruit', 'tree'], cultivarName: 'Example',
      growingNotes: '<img src=x onerror=alert(1)>', harvestInstructions: 'Pick ripe fruit.',
      careSourceUrl: 'https://example.org/care', reviewedOn: '2026-10-25', photo: null });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent.replace(/\s+/g, ' ')).toContain('Fruit · Trees');
    expect(fixture.nativeElement.textContent).toContain('Pick ripe fruit.');
    expect(fixture.nativeElement.textContent).toContain('frost risk');
    expect(fixture.nativeElement.textContent).toContain('<img');
    expect(fixture.nativeElement.querySelector('img')).toBeNull();
    expect(fixture.nativeElement.querySelector('a').getAttribute('href')).toBe('https://example.org/care');
  });
});
