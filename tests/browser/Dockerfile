FROM mcr.microsoft.com/playwright:v1.64.0-noble
WORKDIR /tests
COPY package.json package-lock.json ./
RUN npm ci --ignore-scripts
COPY playwright.config.ts journey.spec.ts sharing.spec.ts pwa.spec.ts pwa-server.mjs generate-icons.mjs accessibility.ts accessibility.spec.ts catalog.spec.ts garden.spec.ts cultivars.spec.ts shortlist.spec.ts photo-journal.spec.ts style-review.spec.ts ui-controls.ts ./
COPY staging.config.ts staging.spec.ts staging-reporter.ts ./
COPY apple.config.ts apple.spec.ts ./
CMD ["npm", "test"]
