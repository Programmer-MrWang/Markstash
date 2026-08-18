plugins {
    id("com.android.library")
}

android {
    namespace = "io.github.programmermrwang.markstash.core.model"
    compileSdk {
        version = release(37) {
            minorApiLevel = 0
        }
    }
    defaultConfig { minSdk = 31 }
    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }
}

kotlin {
    compilerOptions {
        jvmTarget.set(org.jetbrains.kotlin.gradle.dsl.JvmTarget.JVM_17)
    }
}

dependencies {
    implementation(libs.kotlinx.coroutines.core)
    testImplementation(libs.junit)
}
