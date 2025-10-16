import { Component, ChangeDetectionStrategy, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../../auth.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrls: ['./login.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Login {
  private fb = new FormBuilder();
  private auth = inject(AuthService);
  private router = inject(Router);

  isLoading = signal(false);
  errorMessage = signal<string | null>(null);

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  async submit() {
  if (this.form.invalid) return;
  this.isLoading.set(true);
  this.errorMessage.set(null);

  try {
    const { email, password } = this.form.value;
    await this.auth.login({
      email: email ?? '',
      password: password ?? ''
    }).toPromise();
    this.router.navigate(['/dashboard']); 
  } catch {
    this.errorMessage.set('Invalid email or password.');
  } finally {
    this.isLoading.set(false);
  }
}
}