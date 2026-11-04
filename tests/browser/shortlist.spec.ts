import { openCatalogTools } from './ui-controls';
import { expect, test } from '@playwright/test';
import { auditAccessibility } from './accessibility';

test.use({ serviceWorkers: 'block' });

test('private shortlist persists, combines filters, reconciles failed changes and stays separate from owned plants', async ({ page }) => {
  const ids=['40000000-0000-4000-8000-000000000001','40000000-0000-4000-8000-000000000002'];
  const catalog=ids.map((id,index)=>({id,commonName:index?'Tomato (Tiny Tim)':'Tomato (Roma)',
    scientificName:`Solanum lycopersicum '${index?'Tiny Tim':'Roma'}'`,description:'Synthetic crop',
    sunlightRequirement:'FullSun',sunlightInstructions:'Sun',defaultWateringIntervalDays:7,wateringInstructions:'Check moisture',
    defaultFertilizingIntervalDays:null,fertilizingInstructions:null,soilInstructions:'Drained soil',humidityInstructions:null,
    minimumTemperatureCelsius:null,maximumTemperatureCelsius:null,isToxicToPets:false,
    catalogProfile:{cultivarName:index?'Tiny Tim':'Roma',categories:['vegetable'],photo:null,
      careSourceUrl:'https://www.rhs.org.uk/vegetables/tomatoes/grow-your-own',reviewedOn:'2026-10-31'}}));
  const saved=new Set<string>();const unexpected:string[]=[];let failLoad=true,failSave=true,failRemove=true;
  await page.route('**/api/**',async route=>{
    const request=route.request(),path=new URL(request.url()).pathname,method=request.method();
    if(path==='/api/antiforgery/token')return route.fulfill({json:{requestToken:'synthetic'}});
    if(path==='/api/account/me')return route.fulfill({json:{id:'50000000-0000-4000-8000-000000000001',email:'shortlist@example.test'}});
    if(path==='/api/shortlist'&&method==='GET')return route.fulfill(failLoad?{status:503,json:{}}:{json:[...saved].map(plantSpeciesId=>({plantSpeciesId,savedAtUtc:'2026-10-31T12:00:00Z'}))});
    if(path.startsWith('/api/shortlist/')&&['PUT','DELETE'].includes(method)){
      expect(request.headers()['x-xsrf-token']).toBe('synthetic');
      const id=path.split('/').pop()!;expect(ids).toContain(id);
      if(method==='PUT'){
        if(failSave){failSave=false;return route.fulfill({status:503,json:{}});}
        saved.add(id);
      } else {saved.delete(id);if(failRemove){failRemove=false;return route.fulfill({status:503,json:{}});}}
      return route.fulfill({status:204});
    }
    if(method!=='GET'){unexpected.push(path);return route.fulfill({status:405});}
    if(path==='/api/plant-species'){await new Promise(resolve=>setTimeout(resolve,150));return route.fulfill({json:catalog});}
    const plant=catalog.find(p=>path==='/api/plant-species/'+p.id);
    return route.fulfill(plant?{json:plant}:{status:404,json:{}});
  });
  await page.goto('/plants'); await openCatalogTools(page);const notice=page.getByRole('region',{name:'Your plant shortlist',exact:true});
  await expect(notice.getByRole('alert')).toContainText('could not be loaded');
  const saveRoma=page.getByRole('button',{name:'Save Tomato (Roma) to shortlist',exact:true});
  await expect(saveRoma).toBeDisabled();failLoad=false;await notice.getByRole('button',{name:'Refresh shortlist',exact:true}).click();
  await saveRoma.click();await expect(notice.getByRole('alert')).toContainText('could not be confirmed');expect(saved.size).toBe(0);
  await notice.getByRole('button',{name:'Refresh shortlist',exact:true}).click();await saveRoma.click();
  await expect(page.getByRole('button',{name:'Remove Tomato (Roma) from shortlist',exact:true})).toHaveAttribute('aria-pressed','true');
  await page.reload(); await openCatalogTools(page);await expect(page.getByRole('button',{name:'Remove Tomato (Roma) from shortlist',exact:true})).toHaveAttribute('aria-pressed','true');
  await page.getByRole('checkbox',{name:'My shortlist only',exact:true}).check();await expect(page.locator('.plant-card h2')).toHaveText(['Tomato (Roma)']);
  expect(new URL(page.url()).searchParams.get('shortlisted')).toBe('1');
  await page.getByRole('searchbox',{name:'Find a plant',exact:true}).fill('Tiny Tim');await expect(page.locator('.plant-card')).toHaveCount(0);
  await openCatalogTools(page, true);await page.getByRole('button',{name:'Clear filters',exact:true}).click();await expect(page.locator('.plant-card')).toHaveCount(2);expect(saved.size).toBe(1);
  await page.locator('.plant-card').filter({has:page.getByRole('heading',{name:'Tomato (Roma)',exact:true})}).getByRole('link',{name:'View details',exact:true}).click();
  await expect(page.getByRole('heading',{level:1})).toHaveText('Tomato (Roma)');
  await page.getByRole('button',{name:'Remove Tomato (Roma) from shortlist',exact:true}).click();
  await expect(notice.getByRole('alert')).toContainText('could not be confirmed');expect(saved.size).toBe(0);
  await notice.getByRole('button',{name:'Refresh shortlist',exact:true}).click();await expect(saveRoma).toHaveAttribute('aria-pressed','false');
  await saveRoma.click();await notice.getByRole('link',{name:'View full shortlist',exact:true}).click();await openCatalogTools(page, true);
  await expect(page.locator('.plant-card h2')).toHaveText(['Tomato (Roma)']);
  await openCatalogTools(page, true);await page.getByRole('button',{name:'Clear filters',exact:true}).click();
  for(const name of ['Roma','Tiny Tim'])await page.getByRole('checkbox',{name:`Compare Tomato (${name})`,exact:true}).check();
  await notice.getByRole('link',{name:'View full shortlist',exact:true}).click();await openCatalogTools(page, true);
  await expect(page.locator('.plant-card h2')).toHaveText(['Tomato (Roma)']);
  expect(new URL(page.url()).searchParams.get('compare')).toBe(ids.join(','));
  await page.getByRole('link',{name:'Compare selected plants',exact:true}).click();await expect(page.locator('.comparison-card')).toHaveCount(2);
  expect([...new URL(await page.getByRole('textbox',{name:'Comparison link',exact:true}).inputValue()).searchParams.keys()]).toEqual(['compare']);
  await page.getByRole('link',{name:'Back to plant catalog',exact:false}).click(); await openCatalogTools(page, true);await page.getByRole('checkbox',{name:'My shortlist only',exact:true}).check();
  await page.setViewportSize({width:320,height:900});expect(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth)).toBe(true);
  await auditAccessibility(page,'Saved plant shortlist');expect(unexpected).toEqual([]);
});

test('anonymous catalog browsing never reads or writes a private shortlist',async({page})=>{
  let privateRequests=0;
  await page.route('**/api/**',async route=>{
    const path=new URL(route.request().url()).pathname;
    if(path.startsWith('/api/shortlist'))privateRequests++;
    if(path==='/api/antiforgery/token')return route.fulfill({json:{requestToken:'synthetic'}});
    if(path==='/api/plant-species')return route.fulfill({json:[]});
    return route.fulfill({status:401,json:{}});
  });
  await page.goto('/plants?shortlisted=1'); await openCatalogTools(page);await expect(page.getByRole('link',{name:'Log in to save a shortlist',exact:true})).toBeVisible();
  await expect(page.getByRole('button',{name:'Save to shortlist',exact:true})).toHaveCount(0);expect(privateRequests).toBe(0);
});
