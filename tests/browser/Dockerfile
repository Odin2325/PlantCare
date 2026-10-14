FROM mcr.microsoft.com/playwright:v1.64.0-noble
WORKDIR /tests
COPY package.json package-lock.json ./
RUN npm ci --ignore-scripts
COPY playwright.config.ts journey.spec.ts pwa.spec.ts pwa-server.mjs generate-icons.mjs accessibility.ts accessibility.spec.ts ./
COPY staging.config.ts staging.spec.ts staging-reporter.ts ./
CMD ["npm", "test"]
