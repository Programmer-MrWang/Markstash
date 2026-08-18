plugins {
    id("com.android.library")
    id("org.jetbrains.kotlin.plugin.compose")
}

android {
    namespace = "io.github.programmermrwang.markstash.core.designsystem"
    compileSdk {
        version = release(37) {
            minorApiLevel = 0
        }
    }

    defaultConfig { minSdk = 33 }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    buildFeatures { compose = true }

    testOptions.unitTests.isReturnDefaultValues = true
}

kotlin {
    compilerOptions {
        jvmTarget.set(org.jetbrains.kotlin.gradle.dsl.JvmTarget.JVM_17)
    }
}

dependencies {
    api(project(":core:model"))
    api(platform(libs.androidx.compose.bom))
    api(libs.androidx.compose.animation)
    api(libs.androidx.compose.foundation)
    api(libs.androidx.compose.icons)
    api(libs.androidx.compose.material3)
    api(libs.androidx.compose.ui)
    api(libs.miuix.blur)
    implementation(libs.miuix.icons)
    implementation(libs.miuix.shader)
    implementation(libs.miuix.ui)
    implementation(libs.kotlinx.coroutines.android)

    testImplementation(libs.junit)
}
