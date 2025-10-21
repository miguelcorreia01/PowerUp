import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map } from 'rxjs';

export interface Instructor {
  id: string;
  userId: string;
  user?: { name: string; email: string };
}

export interface PtSession {
  id: string;
  instructorId: string;
  memberId: string;
  price: number;
  sessionTime: string;
  status: 'Scheduled' | 'Completed' | 'Cancelled' | 'NoShow';
}

@Injectable({ providedIn: 'root' })
export class PersonalTrainingService {
  private http = inject(HttpClient);
  private baseUrl = 'http://localhost:5255/api';

  getInstructors() {
    return this.http.get<Instructor[]>(`${this.baseUrl}/instructor`);
  }

  getAllSessions() {
    return this.http.get<PtSession[]>(`${this.baseUrl}/ptsession`);
  }

  getMyInstructorSessions(instructorId: string) {
    return this.getAllSessions().pipe(
      map(list => list.filter(s => s.instructorId?.toLowerCase() === instructorId.toLowerCase()))
    );
  }

  getMyMemberSessions(memberId: string) {
    return this.getAllSessions().pipe(
      map(list => list.filter(s => s.memberId?.toLowerCase() === memberId.toLowerCase()))
    );
  }

  bookSession(instructorId: string, sessionTimeISO: string) {
    return this.http.post<{ message: string }>(`${this.baseUrl}/ptsession/book`, {
      instructorId,
      sessionTime: sessionTimeISO
    });
  }

  cancelSession(sessionId: string) {
    return this.http.delete<void>(`${this.baseUrl}/ptsession/${sessionId}`);
  }
}
