package io.github.programmermrwang.markstash

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.lifecycle.viewmodel.compose.viewModel

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        val container = (application as MarkstashApplication).container
        setContent {
            val mainViewModel: MainViewModel = viewModel(
                factory = MainViewModel.factory(container),
            )
            MarkstashApp(viewModel = mainViewModel)
        }
    }
}
