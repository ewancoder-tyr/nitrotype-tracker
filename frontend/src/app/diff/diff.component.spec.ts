import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DebugElement, provideExperimentalZonelessChangeDetection } from '@angular/core';
//import { tryInit } from '../test-init';
import { DiffComponent } from './diff.component';

describe('DiffComponent', () => {
    let component: DiffComponent;
    let fixture: ComponentFixture<DiffComponent>;
    let debug: DebugElement;
    let html: HTMLElement;
    const rootPx = 16;

    beforeEach(async () => {
        //tryInit();
        await TestBed.configureTestingModule({
            providers: [provideExperimentalZonelessChangeDetection()],
            imports: [DiffComponent]
        }).compileComponents();

        fixture = TestBed.createComponent(DiffComponent);
        component = fixture.componentInstance;
        debug = fixture.debugElement;
        html = debug.nativeElement;
    });

    describe('when component is initialized', () => {
        beforeEach(async () => {
            component.value = 5;
            await fixture.whenStable();
        });

        it('should initialize', () => {
            expect(component).toBeDefined();
        });

        it('should have smaller font', () => {
            const expectedFontSize = 0.8; // em.
            expect(getComputedStyle(html).fontSize).toBe(`${rootPx * expectedFontSize}px`);
        });

        it('should not wrap', () => {
            expect(getComputedStyle(html).whiteSpace).toBe('nowrap');
        });
    });

    describe('when value is zero', () => {
        beforeEach(async () => {
            component.value = 0;
            await fixture.whenStable();
        });

        it('should not show anything', () => {
            expect(html.textContent?.replace('\n', '')).toEqual('');
        });
    });

    describe('when value is negative', () => {
        beforeEach(async () => {
            component.value = -10.8888888;
            await fixture.whenStable();
        });

        it('should have bad class', () => {
            expect(html.classList.contains('bad')).toBeTrue();
        });

        it('should show value with arrow down and 4 digits', () => {
            expect(html.textContent).toContain('\u25bc 10.8889');
        });
    });

    describe('when value is positive', () => {
        beforeEach(async () => {
            component.value = 10.88888888;
            await fixture.whenStable();
        });

        it('should have good class', () => {
            expect(html.classList.contains('good')).toBeTrue();
        });

        it('should show value with arrow up and 4 digits', () => {
            expect(html.textContent).toContain('\u25b2 10.8889');
        });
    });
});
