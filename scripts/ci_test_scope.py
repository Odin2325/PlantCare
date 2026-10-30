"""Conservative CI selection. Unknown changes and policy changes require full tests."""
import argparse
import os
import re
import subprocess


CATALOG_FILES = {
    "deploy/catalog.houseplants.json", "deploy/catalog-images.json", "deploy/catalog-cultivar-photos.json",
    "PlantCare.Domain/ValueObjects/PlantCatalogProfile.cs",
    "PlantCare.Domain.Tests/Entities/PlantCatalogProfileTests.cs",
    "PlantCare.Api.IntegrationTests/BootstrapCommandTests.cs",
    "plant-care-web/src/app/models/plant-species.model.ts",
    "plant-care-web/src/app/shared/plant-categories.ts",
    "tests/browser/catalog.spec.ts",
    "tests/browser/cultivars.spec.ts",
}
CATALOG_PREFIXES = (
    "docs/", "plant-care-web/public/images/plants/",
    "plant-care-web/src/app/features/plant-catalog/pages/",
    "plant-care-web/src/app/features/my-plants/pages/my-plant-detail-page/",
    "plant-care-web/src/app/shared/plant-growing-guide/",
)


def select_scope(paths, *, force_full=False, deleted=False):
    if force_full or deleted or not paths:
        return "full"
    return "catalog" if all(
        path in CATALOG_FILES or path.startswith(CATALOG_PREFIXES) for path in paths
    ) else "full"


def git_scope(base, head, force_full=False):
    if force_full or not all(re.fullmatch(r"[a-fA-F0-9]{40}", sha or "") and
                             sha != "0" * 40 for sha in (base, head)):
        return "full"
    try:
        paths = subprocess.check_output(["git", "diff", "--name-only", "-z", base, head, "--"]).decode().split("\0")
        deleted = subprocess.check_output(["git", "diff", "--diff-filter=D", "--name-only", "-z", base, head, "--"])
    except (subprocess.CalledProcessError, UnicodeDecodeError):
        return "full"
    return select_scope([path for path in paths if path], deleted=bool(deleted))


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--base", default=os.getenv("BASE_SHA", ""))
    parser.add_argument("--head", default=os.getenv("HEAD_SHA", ""))
    parser.add_argument("--full", action="store_true")
    args = parser.parse_args()
    scope = git_scope(args.base, args.head, args.full or os.getenv("FORCE_FULL") == "true")
    print(f"Validation scope: {scope}")
    if output := os.getenv("GITHUB_OUTPUT"):
        with open(output, "a", encoding="utf-8") as file:
            file.write(f"scope={scope}\n")
