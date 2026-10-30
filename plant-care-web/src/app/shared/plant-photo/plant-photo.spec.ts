import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { PlantPhoto } from './plant-photo';
import { PlantCatalogProfile } from '../../models/plant-species.model';

const profile: PlantCatalogProfile = {
  careSourceUrl: 'https://example.org/care', reviewedOn: '2026-10-21',
  photo: { path: '/images/plants/monstera-deliciosa.jpg', altText: 'A potted Monstera',
    credit: 'Photographer', sourceUrl: 'https://example.org/photo',
    licenseName: 'CC BY 4.0', licenseUrl: 'https://creativecommons.org/licenses/by/4.0/' },
};

describe('Plant photos and attribution', () => {
  it('shows a named pending placeholder instead of a generic cultivar photo, while preserving explicit reference images', () => {
    const fixture = TestBed.createComponent(PlantPhoto);
    fixture.componentRef.setInput('profile', { ...profile, cultivarName: 'Example', photoIsSpeciesReference: true });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('img')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Cultivar photo pending');
    expect(fixture.nativeElement.querySelector('[role="img"]').getAttribute('aria-label')).toBe('Cultivar photo pending: Example');
    fixture.componentRef.setInput('photo', profile.photo); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.reference-note')).toBeNull();
    expect(fixture.nativeElement.querySelector('img')).not.toBeNull();
    fixture.componentRef.setInput('photo', null);
    fixture.componentRef.setInput('profile', { ...profile, cultivarName: 'Example', photo: null }); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Cultivar photo pending');
  });
  it('retains source/license links when an image fails and retries a different image', () => {
    const fixture = TestBed.createComponent(PlantPhoto);
    fixture.componentRef.setInput('profile', profile); fixture.detectChanges();
    const image = fixture.nativeElement.querySelector('img') as HTMLImageElement;
    expect(image.alt).toBe('A potted Monstera');
    expect(image.getAttribute('loading')).toBe('lazy');
    image.dispatchEvent(new Event('error')); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('img')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Photo unavailable');
    expect(fixture.nativeElement.querySelectorAll('figcaption a').length).toBe(2);
    fixture.componentRef.setInput('profile', { ...profile, photo: { ...profile.photo!, path: '/images/plants/pilea-peperomioides.jpg' } });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('img')).not.toBeNull();
  });
  it('provides an accessible placeholder for legacy species without metadata', () => {
    const fixture = TestBed.createComponent(PlantPhoto); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="img"]').getAttribute('aria-label')).toBe('No plant photo available');
    expect(fixture.nativeElement.querySelector('img')).toBeNull();
  });
});
