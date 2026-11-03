import { expect, test } from '@playwright/test';
import { auditAccessibility } from './accessibility';

test.use({serviceWorkers:'block'});
const id='10000000-0000-4000-8000-000000000001';
const photoId='20000000-0000-4000-8000-000000000001';
const base='/api/my-plants/'+id+'/photos';
const png=Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAgAAAAECAIAAAA8r+mnAAAAEElEQVR4nGNgaGDAjqgnAQAgkRABZnGeigAAAABJRU5ErkJggg==','base64');
// Synthetic JPEG produced by the isolated normalizer from the synthetic PNG above.
const jpeg=Buffer.from('/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAYEBAUEBAYFBQUGBgYHCQ4JCQgICRINDQoOFRIWFhUSFBQXGiEcFxgfGRQUHScdHyIjJSUlFhwpLCgkKyEkJST/2wBDAQYGBgkICREJCREkGBQYJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCT/wAARCAAEAAgDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAf/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/8QAFQEBAQAAAAAAAAAAAAAAAAAABgf/xAAUEQEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIRAxEAPwCcAA6bv//Z','base64');
const plant={id,plantSpeciesId:'30000000-0000-4000-8000-000000000001',nickname:'Balcony tomato',speciesCommonName:'Tomato',speciesScientificName:'Solanum lycopersicum',
  location:'Balcony',acquiredOn:null,notes:null,tags:[],isActive:true,createdAtUtc:'2026-01-01T12:00:00Z',defaultWateringIntervalDays:7,defaultFertilizingIntervalDays:null,careSchedules:[]};

test('photo journal reconciles uncertain saves, preserves edit drafts and confirms deletion',async({page})=>{
  await page.clock.setFixedTime(new Date('2026-11-03T12:00:00Z'));
  let entries:any[]=[],requestId='',posts=0,failEdit=true;const unexpected:string[]=[];
  await page.route('**/api/**',async route=>{
    const request=route.request(),path=new URL(request.url()).pathname,method=request.method();
    if(path==='/api/antiforgery/token')return route.fulfill({json:{requestToken:'synthetic-photo-token'}});
    if(path==='/api/account/me')return route.fulfill({json:{id:'owner-a',email:'photo@example.test'}});
    if(path==='/api/my-plants/'+id&&method==='GET')return route.fulfill({json:plant});
    if(path===base+'/capabilities')return route.fulfill({json:{uploadsEnabled:true,canUpload:true,maximumFileBytes:8388608}});
    if(path===base&&method==='GET')return route.fulfill({json:entries});
    if(path===base&&method==='POST'){
      posts++;expect(request.headers()['x-xsrf-token']).toBe('synthetic-photo-token');
      const body=request.postDataBuffer()!.toString();expect(body).toContain('filename="photo"');expect(body).not.toContain('private-camera.png');
      requestId=/name="requestId"\r\n\r\n([^\r]+)/.exec(body)![1];
      entries=[{id:photoId,userPlantId:id,photographedOn:'2026-11-01',notes:'First leaves',createdAtUtc:'2026-11-03T12:00:00Z',version:'v1'}];
      return route.fulfill({status:503,json:{}}); // Saved, but confirmation was lost.
    }
    if(path===base+'/requests/'+requestId)return route.fulfill({json:entries[0]});
    if(path===base+'/'+photoId+'/thumbnail'||path===base+'/'+photoId+'/image')return route.fulfill({contentType:'image/jpeg',body:jpeg});
    if(path===base+'/'+photoId&&method==='PATCH'){
      expect(request.headers()['x-xsrf-token']).toBe('synthetic-photo-token');
      if(failEdit){failEdit=false;entries[0].version='v2';return route.fulfill({status:409,json:{}});}
      entries[0]={...entries[0],...request.postDataJSON(),version:'v3'};return route.fulfill({json:entries[0]});
    }
    if(path===base+'/'+photoId&&method==='DELETE'){entries=[];return route.fulfill({status:503,json:{}});}
    if(method!=='GET'){unexpected.push(path);return route.fulfill({status:405});}
    return route.fulfill({json:[]});
  });
  await page.goto('/my-plants/'+id);const journal=page.getByRole('region',{name:'Photo journal',exact:true});
  await expect(journal.getByText('No growth photos saved yet.')).toBeVisible();
  await journal.getByLabel('Choose plant photo', {exact:true}).setInputFiles({name:'private-camera.png',mimeType:'image/png',buffer:png});
  await journal.getByLabel('Photo date',{exact:true}).fill('2026-11-01');await journal.getByLabel('Photo notes (optional)',{exact:true}).fill('First leaves');
  page.once('dialog',dialog=>dialog.dismiss());await page.getByRole('link',{name:'Back to calendar',exact:true}).click();
  await expect(page).toHaveURL(new RegExp('/my-plants/'+id+'$'));await expect(journal.locator('.draft-preview')).toBeVisible();
  await journal.getByRole('button',{name:'Save photo',exact:true}).click();
  await expect(journal.getByRole('alert')).toContainText('could not be confirmed');await expect(journal.getByLabel('Photo notes (optional)',{exact:true})).toBeDisabled();
  await journal.getByRole('button',{name:'Check save status',exact:true}).click();await expect(journal.getByText('First leaves',{exact:true})).toBeVisible();expect(posts).toBe(1);
  const viewButton=journal.getByRole('button',{name:'View photo from 2026-11-01',exact:true});
  await viewButton.focus();await page.keyboard.press('Enter');await expect(journal.getByRole('region',{name:'Opened growth photo'})).toBeFocused();
  await page.setViewportSize({width:320,height:900});await journal.scrollIntoViewIfNeeded();
  expect(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth)).toBe(true);await auditAccessibility(page,'Private photo journal');
  if(process.env.PHOTO_JOURNAL_SCREENSHOT)await page.screenshot({path:process.env.PHOTO_JOURNAL_SCREENSHOT});
  await journal.getByRole('button',{name:'Close growth photo',exact:true}).click();await expect(viewButton).toBeFocused();
  await journal.getByRole('button',{name:'Edit photo from 2026-11-01',exact:true}).click();await journal.getByLabel('Edit photo notes',{exact:true}).fill('Grew a leaf');
  await journal.getByRole('button',{name:'Save photo notes',exact:true}).click();await expect(journal.getByRole('alert')).toContainText('could not be confirmed');
  await expect(journal.getByLabel('Edit photo notes',{exact:true})).toHaveValue('Grew a leaf');await journal.getByRole('button',{name:'Refresh photo journal',exact:true}).click();
  page.once('dialog',dialog=>dialog.accept());await journal.getByRole('button',{name:'Cancel photo edit',exact:true}).click();
  await journal.getByRole('button',{name:'Edit photo from 2026-11-01',exact:true}).click();await journal.getByLabel('Edit photo notes',{exact:true}).fill('Grew a leaf');
  await journal.getByRole('button',{name:'Save photo notes',exact:true}).click();await expect(journal.getByText('Grew a leaf',{exact:true})).toBeVisible();
  await page.reload();await expect(journal.getByText('Grew a leaf',{exact:true})).toBeVisible();
  await journal.getByRole('button',{name:'Delete photo from 2026-11-01',exact:true}).click();await journal.getByRole('button',{name:'Keep photo',exact:true}).click();expect(entries).toHaveLength(1);
  await journal.getByRole('button',{name:'Delete photo from 2026-11-01',exact:true}).click();await journal.getByRole('button',{name:'Confirm photo deletion',exact:true}).click();
  await expect(journal.getByRole('alert')).toContainText('Deletion could not be confirmed');await expect(journal.getByText('Grew a leaf',{exact:true})).toBeVisible();
  await journal.getByRole('button',{name:'Refresh photo journal',exact:true}).click();await expect(journal.getByText('No growth photos saved yet.')).toBeVisible();expect(unexpected).toEqual([]);
});

test('photo uploads remain unavailable when disabled and offline changes are never queued',async({page,context})=>{
  let writes=0;
  await page.route('**/api/**',async route=>{
    const path=new URL(route.request().url()).pathname;if(route.request().method()!=='GET'){writes++;return route.fulfill({status:404});}
    if(path==='/api/antiforgery/token')return route.fulfill({json:{requestToken:'synthetic'}});
    if(path==='/api/account/me')return route.fulfill({json:{id:'owner-a',email:'photo@example.test'}});
    if(path==='/api/my-plants/'+id)return route.fulfill({json:plant});
    if(path===base+'/capabilities')return route.fulfill({json:{uploadsEnabled:false,canUpload:false,maximumFileBytes:8388608}});
    return route.fulfill({json:[]});
  });
  await page.goto('/my-plants/'+id);const journal=page.getByRole('region',{name:'Photo journal',exact:true});
  await expect(journal.getByText('Photo uploads are not enabled yet.')).toBeVisible();await expect(journal.locator('input[type=file]')).toHaveCount(0);
  await context.setOffline(true);await expect(journal.getByText('Photo journal needs a connection. Changes are not queued.')).toBeVisible();
  await expect(journal.getByRole('button',{name:'Refresh photo journal',exact:true})).toBeDisabled();await context.setOffline(false);expect(writes).toBe(0);
});
