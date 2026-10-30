import importlib.util
import pathlib
import unittest

spec = importlib.util.spec_from_file_location("scope", pathlib.Path(__file__).parents[1] / "scripts/ci_test_scope.py")
scope = importlib.util.module_from_spec(spec)
spec.loader.exec_module(scope)


class CiScopeTests(unittest.TestCase):
    def test_catalog_content_and_ui_use_focused_checks(self):
        self.assertEqual("catalog", scope.select_scope([
            "deploy/catalog.houseplants.json", "plant-care-web/public/images/plants/fig.jpg",
            "deploy/catalog-cultivar-photos.json", "tests/browser/cultivars.spec.ts",
            "plant-care-web/src/app/features/plant-catalog/pages/plant-catalog-page/plant-catalog-page.ts",
            "PlantCare.Domain/ValueObjects/PlantCatalogProfile.cs"]))

    def test_policy_and_workflows_require_full_validation(self):
        for path in ["scripts/ci_test_scope.py", ".github/workflows/ci.yml", "scripts/Test-LocalBrowserJourney.ps1", "tests/test_ci_test_scope.py"]:
            self.assertEqual("full", scope.select_scope([path]))

    def test_security_persistence_dependencies_and_unknown_changes_require_full(self):
        for path in ["PlantCare.Infrastructure/Persistence/Migrations/new.cs", "PlantCare.Api/Controllers/AuthController.cs",
                     "plant-care-web/src/app/core/auth/services/auth.service.ts", "PlantCare.Worker/ReminderWorker.cs",
                     "plant-care-web/src/app/features/garden/garden-page.ts", "PlantCare.Application/Care/CareService.cs",
                     "plant-care-web/package.json", "compose.yaml", "scripts/Invoke-VpsBackup.ps1", "new-file.txt"]:
            self.assertEqual("full", scope.select_scope(["deploy/catalog-images.json", path]))

    def test_empty_deleted_and_forced_changes_require_full(self):
        self.assertEqual("full", scope.select_scope([]))
        self.assertEqual("full", scope.select_scope(["deploy/catalog-images.json"], deleted=True))
        self.assertEqual("full", scope.select_scope(["deploy/catalog-images.json"], force_full=True))

    def test_invalid_or_missing_git_base_fails_closed(self):
        for base in ["", "0" * 40, "HEAD; echo invalid"]:
            self.assertEqual("full", scope.git_scope(base, "a" * 40))


if __name__ == "__main__":
    unittest.main()
