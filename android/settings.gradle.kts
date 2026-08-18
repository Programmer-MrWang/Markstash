pluginManagement {
    repositories {
        google {
            content {
                includeGroupByRegex("com\\.android.*")
                includeGroupByRegex("com\\.google.*")
                includeGroupByRegex("androidx.*")
            }
        }
        mavenCentral()
        gradlePluginPortal()
    }
}

// Mirrors BiliPai's deterministic local-checkout path for Miuix development.
providers.gradleProperty("markstash.miuix.source").orNull?.let { sourcePath ->
    includeBuild(sourcePath) {
        dependencySubstitution {
            substitute(module("top.yukonga.miuix.kmp:miuix-core-android"))
                .using(project(":miuix-core"))
            substitute(module("top.yukonga.miuix.kmp:miuix-ui-android"))
                .using(project(":miuix-ui"))
            substitute(module("top.yukonga.miuix.kmp:miuix-shader-android"))
                .using(project(":miuix-shader"))
            substitute(module("top.yukonga.miuix.kmp:miuix-blur-android"))
                .using(project(":miuix-blur"))
            substitute(module("top.yukonga.miuix.kmp:miuix-icons-android"))
                .using(project(":miuix-icons"))
        }
    }
}

dependencyResolutionManagement {
    repositoriesMode.set(RepositoriesMode.FAIL_ON_PROJECT_REPOS)
    repositories {
        google()
        mavenCentral()

    }
}

rootProject.name = "MarkstashNative"

include(":app")
include(":core:designsystem")
include(":core:model")
include(":core:platform")
