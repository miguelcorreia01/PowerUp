import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import {BehaviorSubject, Observable, tap, map } from "rxjs";

export interface AuthResponse {
    token: string;
    role: string;
    name: string;
    userId: string;
}

interface BackendAuthResponse {
    Id: string;
    Name: string;
    Email: string;
    Role: string;
    Token: string;
}

@Injectable({providedIn: 'root'})
export class AuthService {
    private baseUrl = 'http://localhost:5255/api';
    private currentUserSubject = new BehaviorSubject<AuthResponse | null>(this.getStoredUser());
    currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient) {}

  // --- REGISTER ---
  register(data: { email: string; password: string; name: string; phoneNumber: string }): Observable<AuthResponse> {
    return this.http.post<BackendAuthResponse>(`${this.baseUrl}/auth/register`, data).pipe(
      map((res) => ({
        token: res.Token,
        role: res.Role,
        name: res.Name,
        userId: res.Id
      })),
      tap((res) => this.storeUser(res))
    );
  }

  // --- LOGIN ---
  login(data: { email: string; password: string }): Observable<AuthResponse> {
    return this.http.post<BackendAuthResponse>(`${this.baseUrl}/auth/login`, data).pipe(
      map((res) => ({
        token: res.Token,
        role: res.Role,
        name: res.Name,
        userId: res.Id
      })),
      tap((res) => this.storeUser(res))
    );
  }

  // --- LOGOUT ---
  logout(): void {
    localStorage.removeItem('user');
    this.currentUserSubject.next(null);
  }

  // --- HELPERS ---
  private storeUser(user: AuthResponse) {
    localStorage.setItem('user', JSON.stringify(user));
    this.currentUserSubject.next(user);
  }

  private getStoredUser(): AuthResponse | null {
    const stored = localStorage.getItem('user');
    return stored ? JSON.parse(stored) : null;
  }

  getToken(): string | null {
    return this.currentUserSubject.value?.token ?? null;
  }

  getRole(): string | null {
    return this.currentUserSubject.value?.role ?? null;
  }

  isLoggedIn(): boolean {
    return !!this.getToken();
  }
}